using System;
using System.Security.Cryptography;
using System.Text;

namespace Hawk.Core.Utils
{
    public class CMDHelper
    {
            public static string GetMD5Value(string password)
            {
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    return BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(password)));
                }
            }

            public static bool IsSame(string password1, string password2)
            {
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    var str1 = BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(password1)));
                    var str2 = BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(password2)));
                    return str1 == str2;
                }
            }
        #region Public Methods

        public static  string Execute(string command)
        {
            var p = new System.Diagnostics.Process();

            //设定程序名

            p.StartInfo.FileName = "cmd.exe";

            //关闭Shell的使用

            p.StartInfo.UseShellExecute = false;
            //重定向标准输入

            p.StartInfo.RedirectStandardInput = true;

            //重定向标准输出

            p.StartInfo.RedirectStandardOutput = true;

            //重定向错误输出

            p.StartInfo.RedirectStandardError = true;

            //设置不显示窗口

            p.StartInfo.CreateNoWindow = true;

            p.Start();

            p.StandardInput.WriteLine(command); //这里就可以输入你自己的命令
            var str= p.StandardOutput.ReadLine();
            p.Close();
            return str;
        }

        #endregion
    }
}