using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls.WpfPropertyGrid.Attributes;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators
{
   

    [XFrmWork("FolderGE","FolderGE_desc" ,"folder_open")]
    public class FolderGE : GeneratorBase
    {
        private List<string> fileList;
        private string _folderPath;
        [Browsable(false)]
        public override string KeyConfig => FolderPath;

        public FolderGE() 
        {
            Pattern = "*.*";
            fileList = new List<string>();
            shouldUpdate = true;
          

            
        }

        private bool shouldUpdate;
        private string _pattern;
        private SearchOption _searchOption;

        [LocalizedDisplayName("key_163")]
        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                if (_folderPath != value)
                {
                    shouldUpdate = true;
                }
                _folderPath = value;
            }
        }

        [LocalizedDisplayName("key_448")]
        [LocalizedDescription("key_449")]
        public string Pattern
        {
            get { return _pattern; }
            set
            {
                if (_pattern != value)
                {
                    shouldUpdate = true;
                    _pattern = value;
                }
            }
        }

        [LocalizedDisplayName("key_450")]
        [LocalizedDescription("key_451")]
        public SearchOption SearchOption
        {
            get { return _searchOption; }
            set
            {
                if (_searchOption != value)
                {
                    shouldUpdate = true;
                }
                _searchOption = value;
            }
        }

        public override bool Init(IEnumerable<IFreeDocument> datas)
        {
            if(string.IsNullOrEmpty(FolderPath))
                return true;

            try
            {
                if(shouldUpdate==true)
                    fileList = Directory.GetFiles(FolderPath, Pattern, SearchOption).ToList();
           
            }
            catch (Exception ex)
            {
                XLogSys.Print.Error(ex.Message);

             
            }
            return true;
        }
        public override int? GenerateCount()
        {
            return fileList.Count;
        }
        public override IEnumerable<IFreeDocument> Generate(IFreeDocument document = null)
        {
           
            foreach (string doc in fileList)
            {
               
                var item= new FreeDocument();

                item.Add(Column, doc);
           
                yield return item;
            }
        }
     
    }
}