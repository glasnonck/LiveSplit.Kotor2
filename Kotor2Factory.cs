using System.Reflection;
using LiveSplit.Kotor2;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

[assembly: ComponentFactory(typeof(Kotor2Factory))]

namespace LiveSplit.Kotor2
{
    class Kotor2Factory : IComponentFactory
    {
        public string ComponentName
        {
            get { return "Kotor2"; }
        }

        public string Description
        {
            get { return "Automates load removal for Star Wars: Knights of the Old Republic 2 - The Sith Lords."; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Control; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new Kotor2Component(state);
        }

        public string UpdateName
        {
            get { return this.ComponentName; }
        }

        public string UpdateURL
        {
            get { return "https://raw.githubusercontent.com/glasnonck/LiveSplit.Kotor2/master/"; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string XMLURL
        {
            get { return this.UpdateURL + "Components/update.LiveSplit.Kotor2.xml"; }
        }
    }
}
