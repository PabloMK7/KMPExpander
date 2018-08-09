using LibCTR.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace KMPExpander.Class.SimpleKMPs
{
    public interface ISectionBase
    {
        object Add();
        void Add(object entry);
        void Remove(object entry);
        object Insert(int row);
        void Insert(int row, object entry);
        object GetEntries();
        void New();
        int IndexOf(object entry);
        int GetCount();
        bool GetVisibility();
        void SetVisibility(bool visible);
    }


    public class SectionBase<T> : ISectionBase where T : new()
    {
        public List<T> Entries = new List<T>();
        [XmlIgnore]
        public bool Visible = true;

        public object Add()
        {
            T entry = new T();
            Entries.Add(entry);
            return entry;
        }

        public void Remove(object entry)
        {
            Entries.Remove((T)entry);
        }

        public void New()
        {
            Entries = new List<T>();
        }

        public void Add(object entry)
        {
            Entries.Add((T)entry);
        }

        public object GetEntries()
        {
            return Entries;
        }

        public void Insert(int row, object entry)
        {
            Entries.Insert(row, (T)entry);
        }

        public object Insert(int row)
        {
            T entry = new T();
            Entries.Insert(row,entry);
            return entry;
        }

        public int IndexOf(object entry)
        {
            return Entries.IndexOf((T)entry);
        }

        public int GetCount()
        {
            return Entries.Count;
        }

        public bool GetVisibility()
        {
            return Visible;
        }

        public void SetVisibility(bool visible)
        {
            Visible = visible;
        }
    }
}
