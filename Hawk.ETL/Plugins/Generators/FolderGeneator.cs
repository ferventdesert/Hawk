using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Hawk.Core.Connectors;
using Hawk.Core.Utils.Logs;
using Hawk.Core.Utils.Plugins;
using Hawk.ETL.Interfaces;

namespace Hawk.ETL.Plugins.Generators
{
   

    [XFrmWork("获取文件夹文件","获取文件夹下的所有文件" )]
    public class FolderGE : GeneratorBase
    {
        private List<string> fileList;
        private string _folderPath;


        public FolderGE() 
        {
            Pattern = "*.*";
            fileList = new List<string>();
            shouldUpdate = true;
          

            
        }

        private bool shouldUpdate;
        private string _pattern;
        private SearchOption _searchOption;

        [DisplayName("路径")]
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

        [DisplayName("筛选模式")]
        [Description("符合windows的文件筛选规范")]
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

        [DisplayName("是否递归")]
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
        public override IEnumerable<FreeDocument> Generate(IFreeDocument document = null)
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