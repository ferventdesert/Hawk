
### 开发者: 沙漠之鹰(desert)


Hawk: Advanced Crawler& ETL tool written in C#/WPF
---

欢迎使用Hawk! HAWK无需编程，可见即所得的图形化数据采集和清洗工具，依据GPL协议开源。

项目主页： https://ferventdesert.github.io/Hawk/

下载地址: 

[Github](https://github.com/ferventdesert/Hawk/releases/download/Hawk3/Hawk3_Release.zip)

[国内博客园](https://files.cnblogs.com/files/buptzym/Hawk3_Release.zip)



## 1.介绍

Hawk的含义为“鹰”，能够高效，准确地捕杀猎物。它的思想来源于Lisp语言，功能模仿了Linux工具awk。

特点如下：
- 智能分析网页内容，无需编程
- 所见即所得，可视化拖拽，快地实现转换和过滤等数据清洗操作
- 能从各类数据库和文件实现导入导出
- 任务可以被保存和复用
- 其最适合的领域是爬虫和数据清洗，但其威力远超于此。

HAWK使用C# 编写，其前端界面使用WPF开发，因此只能运行于windows平台，但提供命令行入口供自动化部署。
以下介绍全部基于最新的Hawk3，请使用老版本的同学尽快通过下面的地址升级最新版。

![2.gif-3330.9kB][1]

以获取大众点评的所有北京美食为例，使用本软件可在10分钟内完成配置，在1小时之内**自动并行抓取**全部内容，并能监视任务工作情况。而手工编写代码，即使是使用python，一个熟练的程序员也可能需要一天以上：

![1.gif-1001.8kB][2]

## 2. 文档和下载地址

GitHub地址：https://github.com/ferventdesert/Hawk

示例工程文件： https://github.com/ferventdesert/Hawk-Projects

文档地址: https://github.com/ferventdesert/Hawk/wiki

编译： 下载VS2015及以上版本，解决方案路径在Hawk.Core\Hawk.Core.sln

## 3. 更新历史

- 2012 开始开发 
- 2016.4 Hawk1开源发布
- 2016.10 Hawk2发布   支持动态嗅探和超级模式，修复bug
- 2018.5 Hawk3 交互极大优化，增强子任务,支持sqlite等。

其Python类似的实现是etlpy:但由于Hawk更新频繁，Hawk3无法再兼容，因此etlpy仅供参考。

> http://www.cnblogs.com/buptzym/p/5320552.html


## 4. 视频演示

友情提示：由于软件更新频繁，界面有较大变化。但Hawk1到Hawk3在操作思路上基本一致，只是文案和布局上有少许区别，因此老版本的视频同样可做重要的参考资料。

- Hawk3

[Hawk3视频教学字幕版-新功能展示][8]

- Hawk1

[链家二手房][3]

[微信公共平台][4]

[大众点评-北京美食][5]

- Hawk2

[抓取动态页面][6]

[Hawk答疑][7]



  [1]: http://static.zybuluo.com/buptzym/10kykg6qhqvsabbq8yj32pt0/2.gif
  [2]: http://static.zybuluo.com/buptzym/qkl0vavjn6cj007qfk2k3gqg/1.gif
  [3]: http://v.qq.com/page/w/9/2/w0189607h92.html
  [4]: http://v.qq.com/page/c/s/n/c0189jwd2sn.html
  [5]: http://v.qq.com/page/z/g/h/z01891n1rgh.html
  [6]: https://v.qq.com/x/page/a03878tihmx.html
  [7]: https://v.qq.com/x/page/n0387axmgg5.html
  [8]: http://v.qq.com/x/page/d0660sxbebu.html
