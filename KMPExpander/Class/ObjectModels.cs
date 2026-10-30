using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KMPExpander.Class
{
    // The OBJ models of the course objects, from a folder of extracted models. An object's model is <name>.obj anywhere in
    // the folder, or else an OBJ inside a folder called <name>, <name> being the object's internal name in ObjFlow.xml.
    // Models are read when first drawn.
    public class ObjectModels
    {
        public readonly string Folder;
        private Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, OBJWrapper> loaded = new Dictionary<string, OBJWrapper>(StringComparer.OrdinalIgnoreCase);

        public ObjectModels(string folder)
        {
            Folder = folder;
            string[] objs = Directory.GetFiles(folder, "*.obj", SearchOption.AllDirectories);
            foreach (string f in objs)
            {
                string dir = Path.GetFileName(Path.GetDirectoryName(f));
                if (!files.ContainsKey(dir)) files[dir] = f;
            }
            foreach (string f in objs) files[Path.GetFileNameWithoutExtension(f)] = f;
            ModelCount = objs.Length;
        }

        public int ModelCount { get; private set; }

        // The skyboxes (ObjFlow.bin flag 0x8000) would cover the whole map; in the retail ObjFlow.bin exactly the objects
        // with that flag have a name ending in VR and a number.
        public static bool IsSkybox(string name)
        {
            return Regex.IsMatch(name, @"VR\d*$");
        }

        // null when there is no model for the name, or it cannot be read.
        public OBJWrapper Get(string name)
        {
            if (String.IsNullOrEmpty(name) || IsSkybox(name)) return null;
            OBJWrapper model;
            if (loaded.TryGetValue(name, out model)) return model;
            string path;
            if (files.TryGetValue(name, out path))
            {
                model = new OBJWrapper(path, true);
                if (model.Failed) model = null;
            }
            loaded[name] = model;
            return model;
        }
    }
}
