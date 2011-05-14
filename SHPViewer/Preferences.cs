using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using OpenRA.FileFormats;
using System.Text;
using System.Drawing;
using OpenRA.FileFormats.Graphics;
using SHPViewer;

namespace SHPViewer
{
    

    public class ShpViewerSettings
	{

        public string LastPalette = Path.Combine(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "assets"), "unittem.pal");
        public string BackgroundFile = "null";
        public ImageLayout BackgroundLayout = (ImageLayout)1;
        public bool TransparentColors = false;
        public bool RemapableColors = false;
        public bool UseShadow = false;
        public bool UseTurret = false;
        public Color RemapColor = Color.Gold;
        public bool ContinousPlaying = false;
        public int TurretOffsetX = 0;
        public int TurretOffsetY = 0;
        public PaletteFormat PaletteFormat = (PaletteFormat)3;
	}

    public class Preferences
	{
		string SettingsFile;
        public ShpViewerSettings ShpViewer = new ShpViewerSettings();
		
		Dictionary<string, object> Sections;
        public Preferences(string file)
		{

            SettingsFile = file;
			Sections = new Dictionary<string, object>()
			{
				{"ShpViewer", ShpViewer}
			};
			
			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;
			
			FieldLoader.UnknownFieldAction = (s,f) =>
			{
				//System.Console.WriteLine( "Ignoring unknown field `{0}` on `{1}`".F( s, f.Name ) );
			};
			
			if (File.Exists(SettingsFile))
			{
				//System.Console.WriteLine("Loading settings file {0}",SettingsFile);
				var yaml = MiniYaml.DictFromFile(SettingsFile);
				
				foreach (var kv in Sections)
					if (yaml.ContainsKey(kv.Key))
						LoadSectionYaml(yaml[kv.Key], kv.Value);
			}
			
			
			FieldLoader.UnknownFieldAction = err1;
			FieldLoader.InvalidValueAction = err2;
		}
		
		public void Save()
		{
			var root = new List<MiniYamlNode>();
			foreach( var kv in Sections )
				root.Add( new MiniYamlNode( kv.Key, SectionYaml( kv.Value ) ) );
			
			root.WriteToFile(SettingsFile);
		}
		
		MiniYaml SectionYaml(object section)
		{
			return FieldSaver.SaveDifferences(section, Activator.CreateInstance(section.GetType()));
		}
		
		void LoadSectionYaml(MiniYaml yaml, object section)
		{
			object defaults = Activator.CreateInstance(section.GetType());
			FieldLoader.InvalidValueAction = (s,t,f) =>
			{
				object ret = defaults.GetType().GetField(f).GetValue(defaults);
				//System.Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s,t.Name,f,ret) );
				return ret;
			};
			
			FieldLoader.Load(section, yaml);
		}
	}
}
