﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.Kotor2
{
    public class GameMemory
    {
        public event EventHandler OnLoadStarted;
        public event EventHandler OnLoadFinished;

        private Task _thread;
        private CancellationTokenSource _cancelSource;
        private SynchronizationContext _uiThread;
        private List<int> _ignorePIDs;

        private DeepPointer _isNotLoadSavePtr;
        private DeepPointer _isActiveWindowPtr;
        private DeepPointer _isMoviePlayingPtr;

        // private enum ExpectedDllSizes
        // {
        //     Kotor2Steam = 4395008,
        // }

        public GameMemory()
        {
            _isNotLoadSavePtr = new DeepPointer("dinput8.dll", 0x2C1D4); // == 1 if (not saving or loading) && swkotor2 is the active window
            _isActiveWindowPtr = new DeepPointer(0x428518); // == 1 if swkotor2 is the active window
            _isMoviePlayingPtr = new DeepPointer(0x428798); // == 1 if a movie is playing

            _ignorePIDs = new List<int>();
        }

        public void StartMonitoring()
        {
            if (_thread != null && _thread.Status == TaskStatus.Running)
            {
                throw new InvalidOperationException();
            }
            if (!(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
            {
                throw new InvalidOperationException("SynchronizationContext.Current is not a UI thread.");
            }

            _uiThread = SynchronizationContext.Current;
            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(MemoryReadThread);
        }

        public void Stop()
        {
            if (_cancelSource == null || _thread == null || _thread.Status != TaskStatus.Running)
            {
                return;
            }

            _cancelSource.Cancel();
            _thread.Wait();
        }

        void MemoryReadThread()
        {
            Debug.WriteLine("[NoLoads] MemoryReadThread");

            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    Debug.WriteLine("[NoLoads] Waiting for swkotor2.exe...");

                    Process game;
                    while ((game = GetGameProcess()) == null)
                    {
                        Thread.Sleep(250);
                        if (_cancelSource.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    Debug.WriteLine("[NoLoads] Got swkotor2.exe!");

                    uint frameCounter = 0;

                    bool prevIsNotLoadSave = false;
                    bool loadSaveStarted = false;

                    while (!game.HasExited)
                    {
                        bool isMoviePlaying;
                        bool isActiveWindow;
                        bool isNotLoadSave;
                        _isMoviePlayingPtr.Deref(game, out isMoviePlaying);
                        _isActiveWindowPtr.Deref(game, out isActiveWindow);
                        _isNotLoadSavePtr.Deref(game, out isNotLoadSave);


                        if (!isMoviePlaying && isActiveWindow)
                        {
                            if (isNotLoadSave != prevIsNotLoadSave)
                            {
                                if (!isNotLoadSave)
                                {
                                    Debug.WriteLine(String.Format("[NoLoads] Load Start - {0}", frameCounter));

                                    loadSaveStarted = true;

                                    // pause game timer
                                    _uiThread.Post(d =>
                                    {
                                        if (this.OnLoadStarted != null)
                                        {
                                            this.OnLoadStarted(this, EventArgs.Empty);
                                        }
                                    }, null);
                                }
                                else
                                {
                                    Debug.WriteLine(String.Format("[NoLoads] Load End - {0}", frameCounter));

                                    if (loadSaveStarted)
                                    {
                                        loadSaveStarted = false;

                                        // unpause game timer
                                        _uiThread.Post(d =>
                                        {
                                            if (this.OnLoadFinished != null)
                                            {
                                                this.OnLoadFinished(this, EventArgs.Empty);
                                            }
                                        }, null);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // unpause game timer
                            _uiThread.Post(d =>
                            {
                                if (this.OnLoadFinished != null)
                                {
                                    this.OnLoadFinished(this, EventArgs.Empty);
                                }
                            }, null);
                        }

                        prevIsNotLoadSave = isNotLoadSave;
                        frameCounter++;

                        Thread.Sleep(15);

                        if (_cancelSource.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }
        }

        Process GetGameProcess()
        {
            Process game = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.ToLower() == "swkotor2"
                && !p.HasExited && !_ignorePIDs.Contains(p.Id));
            if (game == null)
            {
                return null;
            }

            Debug.WriteLine(String.Format("[NoLoads] Found Kotor2 with size {0}", game.MainModule.ModuleMemorySize));

            // if (game.MainModule.ModuleMemorySize != (int)ExpectedDllSizes.KotorSteam && game.MainModule.ModuleMemorySize != (int)ExpectedDllSizes.KotorCracked)
            // {
            //     _ignorePIDs.Add(game.Id);
            //     _uiThread.Send(d => MessageBox.Show("Unexpected game version. Kotor 1.4.651.0 is required.", "LiveSplit.Kotor",
            //         MessageBoxButtons.OK, MessageBoxIcon.Error), null);
            //     return null;
            // }

            return game;
        }
    }
}
