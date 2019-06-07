
# 案例2_大众点评

本文将讲解通过Hawk，获取大众点评的所有美食数据，可选择任一城市，也可以很方便地修改成获取其他生活门类信息的爬虫。

本文将省略原理，**一步步地介绍如何在20分钟内完成爬虫的设计**，基本不需要编程，还能自动并行抓取。

看完这篇文章，你应该就能举一反三地抓取绝大多数网站的数据了。Hawk是一整套工具，它的能力取决于你的设计和思路。希望你会喜欢它。

详细过程视频可参考：http://v.qq.com/page/z/g/h/z01891n1rgh.html，值得注意的是，由于软件不断升级，因此细节和视频可能有所出入。
准备好了么？Let's do it!

## 1.做饭先生火：自动设置cookie：

我们先打开大众点评的美食列表页面：

http://www.dianping.com/search/category/2/10/g311

![image_1airusse2977232s23148o1pi9.png-224.2kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1airusse2977232s23148o1pi9.png-224.2kB.png)

这是北京的"北京菜"列表，但你会注意到，只能抓取前50页数据（如箭头所示），是一种防爬虫策略，我们之后来破解它。

我们双击打开一个网页采集器：

![新建采集器](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/新建采集器.png)

之后在最上方的地址栏里填写地址：

![image_1airv1slp1qnb1abtginug2kc913.png-20.2kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1airv1slp1qnb1abtginug2kc913.png-20.2kB.png)

**(新版本的Hawk已经能自动模拟浏览器，对大众点评，自动嗅探可以忽略)**

但会发现远程服务器拒绝了请求，原因是大众点评认为Hawk是爬虫而不是浏览器。

没有关系，我们让Hawk来监控浏览器的行为，在右侧的**搜索字符**中，填写"王府半岛酒店"（填什么没关系，只要是当前浏览器上随便某个店铺名称就好），之后后Hawk会弹出提示启动自动嗅探，单击`确认`。浏览器会自动打开网页，在浏览器上刷新刚才的页面，程序后台自动记录了所有的行为。


(此处大概介绍原理：Hawk在点击开始之后，会自动成为代理，所有的浏览器请求都会经过Hawk，在输入特定的URL筛选前缀和关键字，则Hawk会自动拦截符合要求的Request，并将其详细信息记录下来，并最终模拟它)。

之后，我们点击右方的“**高级设置**”里，能够看到Hawk已经把这次访问的cookie和headers自动保存下来：

![image_1airves3k7bs44uo0112o01l381t.png-336.8kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1airves3k7bs44uo0112o01l381t.png-336.8kB.png)

此时你会发现Hawk自动勾选了`超级模式`，在大众点评里，是不需要勾选它的，我们手工将它取消掉。

我们再次点击**刷新网页**，可以看到已经能成功获取网页内容：

![image_1airvksaq19ac1ml84d92661rff2a.png-259.1kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1airvksaq19ac1ml84d92661rff2a.png-259.1kB.png)

完成这一步之后，我们就能够像普通网页那样免登陆抓取信息了。这也适合需要登录的各类网站。

## 洗菜切菜：获取门店列表

我们通过自动和手动两种方式来获取门店列表，你可以两种都试试。

### 全自动获取

直接点击**手气不错**即可，不需要其他操作：

(在链家的教程中已经有详细介绍，此处跳过)

### 纯手工获取

我们先手工输入筛选条件吧：

![image_1airusse2977232s23148o1pi9.png-224.2kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1airusse2977232s23148o1pi9.png-224.2kB.png)

输入上面的关键字3774，命名为**点评**：

![image_1airvuu8ef54a78mmd7anvit4h.png-37.3kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1airvuu8ef54a78mmd7anvit4h.png-37.3kB.png)

接着，填入89,你会发现是下面这样：

![image_1ais0256617201p1d1ilv1na6rur4u.png-31.8kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1ais0256617201p1d1ilv1na6rur4u.png-31.8kB.png)

注意XPath表达式和点评的表达式不大一样，这是因为89太普通，在网页中出现多次，再次点击**继续搜索**，即可找到正确的位置。

类似地，将所有你认为需要的属性添加进去，加上合适的命名，大概是这个样子：

![所有属性列表](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/所有属性列表.png)

当然，我们还需要门店的ID，但在页面上并不提供，那在浏览器上点击那个“四季民福烤鸭店”（沙漠君厌恶吃鸭子），你会看到它的链接为：

![image_1ais0br53kf7175t7du1pm210fo65.png-13.3kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1ais0br53kf7175t7du1pm210fo65.png-13.3kB.png)

那么将这个id也输入到搜索属性中，继续添加。


之后获取了全部属性后，点击**提取测试**，系统会自动优化XPath，叶子节点的根路径会显示在下方。

*笔者建议自动加手动配合的方式，自动抓取大部分数据，再用手动修改调整，手气不错虽然智能，但并不是什么时候都管用*。

将本模块命名为**大众点评门店列表**，供之后备用：

![修改任务名称](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/修改任务名称.png)

## 餐前甜点：获取50页数据

我们先用50页数据试试手，在刚才那个浏览器页面的最下方，点击翻页，可以发现是如下的结构：
```
http://www.dianping.com/search/category/2/10/g311p2
http://www.dianping.com/search/category/2/10/g311p3
http://www.dianping.com/search/category/2/10/g311p4
...
```

好，新建**数据清洗**，随便给它起个名字，从左面拖入**生成区间数**，双击配置列名为page，最大值填50，再拖入**合并多列**到page列，配置如下：

![image_1aiu7pia11jtv1fs1crf1eu41fg39.png-40.4kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiu7pia11jtv1fs1crf1eu41fg39.png-40.4kB.png)

其中Format设置为：

`http://www.dianping.com/search/category/2/10/g311p{0}`

这是C#的一种字符串替换语法，{0}会被依次替换为1,2,3...

最后，拖**从爬虫转换**到url列，选择刚才的`大众点评门店列表`，奇迹出现了吧？

为了保存结果，我们拖**写入数据表**到**任意一列**,这里选择了**名称列**，配置如下：

![image_1aiu81b8s1jic1eu3vnp1m5517i1m.png-51.8kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiu81b8s1jic1eu3vnp1m5517i1m.png-51.8kB.png)

之后，在左侧选择并行或串行模式（随你），点击**执行**即可。

![image_1aiu84m21n836rr12jgqgt1b4b13.png-183.8kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiu84m21n836rr12jgqgt1b4b13.png-183.8kB.png)

数据采集完成了！


*如果看到这一步累了，可以不看下面的内容，但如果想获取全部内容，步骤就复杂多了，如果你下决心学习，我们接着往下看*

## 准备葱蒜：获取城市的美食门类

解决问题的办法是分而治之，获取每个区县（如北京的海淀区）下的某一种美食门类（东北菜），自然就没50页那么多了。所以，要获取美食门类，再获取所有的区域。

先找到所有美食门类的位置：

`http://www.dianping.com/shopall/2/0`

为了获取此页面上的信息，我们再新建一个**网页采集器**，命名为**大众点评通用采集**，它的目标是获取整个HTML页面，因此

**读取模式**改成One,将共享源设置为刚才的**门店列表**， 此时两个采集器会共享同一套请求参数。(其实也可以再做一次嗅探，但这个更快也更方便)。

之后，我们来获取这个页面上的所有美食门类，新建**数据清洗**，命名为**门类**，然后从左侧拖**从文本生成**到右侧任意一列，命名如下：

![image_1aistmi1i1fibku21ni1loc66s2n.png-21.9kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aistmi1i1fibku21ni1loc66s2n.png-21.9kB.png)

再拖入**从爬虫转换**，配置如下：

![image_1aistobto1jstlqfduiv7aihq34.png-23.9kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aistobto1jstlqfduiv7aihq34.png-23.9kB.png)

即可调用刚才的**通用采集器**。另外，左侧的工具栏支持搜索，直接关键字即可快速定位，结果如下：

![image_1aistqm3s333b775vr1fsvif3h.png-25.2kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aistqm3s333b775vr1fsvif3h.png-25.2kB.png)

![image_1aistrkufs37jsm1o9c1opsgv53u.png-56.7kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aistrkufs37jsm1o9c1opsgv53u.png-56.7kB.png)

为了获取下图的**北京菜**所在的位置，虽然可以用Hawk，但为了方便可以使用Chrome，搜狗和360浏览器的F12开发者工具功能，找到对应的元素，点击右键，拷贝XPath:

![image_1aisuaq7ib2k8blb5h3r51p5q4b.png-23kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aisuaq7ib2k8blb5h3r51p5q4b.png-23kB.png)

内容为：`//*[@id="top"]/div[6]/div/div[1]/dl[1]/dd/ul/li[1]`，

因为要获取所有的子li，在刚才的数据清洗中，向Content列拖入**XPath筛选器**，配置如下：

![image_1aisui6polg49f134f1rvo8s94o.png-28.3kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aisui6polg49f134f1rvo8s94o.png-28.3kB.png)

由于要获取所有的li子节点，所以去掉了最后的[1]，可以适当复习XPath语法。

奇迹出现了：

![image_1aisujo637p1iptteav57ct55.png-55.6kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aisujo637p1iptteav57ct55.png-55.6kB.png)

接下来步骤很简单，我不截图了：

 - 拖入**HTML字符转义**到Text列，可以清除该列的乱码
 - 再拖入**字符串分割**到Text，勾选**空格分割**，可对该数据用空格分割，并获取默认的第一个子串
 - 拖入**删除该列**到OHTML,该列没有用
 - 再拖入**正则转换器**到HTML，配置如下：
![image_1aiu937op1opn1u6a19tr1fppd8h19.png-42.7kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiu937op1opn1u6a19tr1fppd8h19.png-42.7kB.png)
 g\d+代表匹配那个门类的ID，比如刚才的g311
 
 - 拖入**删除该列**到HTML
 - 直接在Text列的上方修改名称为**门类**
 
最终结果如下：

![image_1aiu979h42tg9pg1uembp81fip1m.png-31.4kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiu979h42tg9pg1uembp81fip1m.png-31.4kB.png)

## 获取北京的区域

这一步和上一步非常类似，因此我很简明地介绍一下。

区域在这个页面：

`http://www.dianping.com/search/category/2/10/g311p2`
![image_1aiu9bfftljg1lmi63h18d7r7823.png-48.1kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiu9bfftljg1lmi63h18d7r7823.png-48.1kB.png)

这些节点的XPath是：//*[@id="region-nav"]/a

你可以按照刚才类似的步骤进行，也是创建新的**数据清洗**，把这个子模块命名为**区域**，最终结果如下：

![image_1aiu9omeh1k1bf9pp1g10v51b3330.png-46.7kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiu9omeh1k1bf9pp1g10v51b3330.png-46.7kB.png)

如果自己做不下来，也没有关系，加载Github上大众点评的`教程.xml`，可以直接用这个现成的模块，也可以单步调试之，看看它是怎么写的。

##  正菜开始：主流程

下面是最难也是最复杂的部分。我们的思路是，组合所有的门类和区域，构成m*n的一组序对，如`海淀区-北京菜`，`朝阳区-火锅`等等，获取对应序对的页数，再将所有结果拼接起来。
准备好了么？我们继续。


新建**数据清洗**，命名为**主流程**，我们要调用刚才定义的模块，拖入**子流-生成**到任意一列，配置如下：

![image_1aiua4trfd641198v121hpt1cma3t.png-26.1kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiua4trfd641198v121hpt1cma3t.png-26.1kB.png)

记得要勾选**启用**，这些模块默认是不启用的。


再拖入**子流-生成**到任意一列，配置如下：

![image_1aiua70q350rgop1tljiud1ovk4a.png-54.4kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiua70q350rgop1tljiud1ovk4a.png-54.4kB.png)

注意生成模式改为Cross。

具体不同模式的工作方式，可参考这篇文章：http://www.cnblogs.com/buptzym/p/5501003.html

之后，就是这个样子：

![image_1aiua9pco128h1qsanbe1g3u12i54n.png-97.6kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiua9pco128h1qsanbe1g3u12i54n.png-97.6kB.png)

我们将两列组合起来，可看到Url为如下的形式：

http://www.dianping.com/search/category/2/10/**g311r14**

拖**合并多列**到type，配置如下：

![image_1aiuaeshc14pib744751r5vfp554.png-27.6kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiuaeshc14pib744751r5vfp554.png-27.6kB.png)

{0}{1}相当于组合了多个元素，拖入的当前列为第0元素，其他项用空格分割，分别代表第1,2...个元素。

为了获取每个门类的页数，需要在页面上找一下：

![image_1aiuajl6nhdrdgh1t3sikl3a95h.png-120.9kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiuajl6nhdrdgh1t3sikl3a95h.png-120.9kB.png)

它的XPath是`/html[1]/body[1]/div[6]/div[1]/span[7]`

 
 - 拖入**从爬虫转换**到url列，配置爬虫选择为**通用采集器**，就能获取对应的HTML
 - 拖入**XPath筛选器**到HTML所在的Content列，XPath表达式如上`/html[1]/body[1]/div[6]/div[1]/span[7]`。只获取一个数据，新列名为count
 - 拖入**删除该列**到Content列。
![image_1aiub6nl319v01bo714su63p1gkq5u.png-65.5kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiub6nl319v01bo714su63p1gkq5u.png-65.5kB.png)
 - 拖入**提取数字**到count列
 - 拖入**Python转换器**到count列，这是本文唯一要写的代码：
 配置如下：
![image_1aiubbcsg1qeg1l0712u216gu1me46b.png-28.8kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiubbcsg1qeg1l0712u216gu1me46b.png-28.8kB.png)
代码在下面：
```
v=int(value)
50 if v>50 else int(v/15)+1
```
Python代码很好理解吧，大概是说超过50页就按50页处理，页数等于数量除以每页15个，取整后+1。

![image_1aiubggfv15qt1ot719b5d3l1de69.png-64.2kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiubggfv15qt1ot719b5d3l1de69.png-64.2kB.png)

你会发现即使这样，每个门类还是超过了50页，这个问题我们之后再讨论。

为了方便并行，拖入**流实例化**到任意一列，配置如下：


执行器会将每一个门类区县对分配一个独立的线程，注意方括号[url]的写法，系统会把url列的内容赋值到这里，如果你只写url，那所有的线程名称都叫url了。你可以不添加流实例化，看看系统最后是怎么工作的。

接下来，我们要把page列展开，生成[0-page]的区间数，一页一页去抓取。拖**生成区间数**到page列，配置如下：

![image_1aiubrrau2rpdb61m7rcno1si113.png-38.8kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiubrrau2rpdb61m7rcno1si113.png-38.8kB.png)

注意Cross和[page]，我就不多解释了。

把刚才的url和现在的p列合并，就构成了每一页的真实url.

拖入**合并多列**到url，配置如下：

![image_1aiuc0p8pv4p18ks1rts1nfh1ivf1t.png-19.4kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiuc0p8pv4p18ks1rts1nfh1ivf1t.png-19.4kB.png)

仔细理解一下配置的意思，尤其是{0}p{1}，我觉得读者到了这一步，已经对整个系统有点感觉了。

雄关漫道真如铁，我们马上到达目的地了。

现在url列已经是这个样子了（点击**查看样例**即可）

![image_1aiuc41shkp2cju10rgrji1h942q.png-26.7kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiuc41shkp2cju10rgrji1h942q.png-26.7kB.png)

将**从爬虫转换**到url，配置爬虫来源为**门店列表**！然后等待奇迹出现

（卖个关子，我就不截图了）

然后拖入**写入数据表**到任意一列，为表名起个名字，点击执行去跑就可以了。

如果你到这一步就满意了，那么文章可以不用往下看了。

## 注重细节

一道大菜要非常注意细节，爬虫也一样。

### 保留原始表的信息

你看到数据表里没有这家店的区县，也没有所在的页数，感觉从爬虫转换丢失了原始表的一部分信息，事实上它在1转多的时候，原始表默认都会丢掉。

因此在下图的位置，点击编辑集合，选择最后的那个从爬虫转换，配置如下：

![image_1aiucj94dqir1rlt19n51n41ue63k.png-6.9kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiucj94dqir1rlt19n51n41ue63k.png-6.9kB.png)

它会将p和区域两列，添加到新表中。

### 我想写入数据库

目前Hawk没有强的自动建表功能，因此建议使用MongoDB,如果你已经安装了，在**模块管理**的数据源哪里，点击右键，可新建MongoDB连接器。

可以在主流程的最后位置，在拖入**写入数据库**，即可。

### 还是没有获取所有数据

即使是刚才这样的复杂操作，依然不能获取所有的美食，因为火锅太火，朝阳海淀的火锅都超过了50页，解决方法是再细分商区，比如朝阳的三元桥，国贸，望京...这样就能完整解决了。但本文限于篇幅就不讨论了。

### 如何将数据表导出到文件？

在右下角的数据管理，在要导出的表上点右键，建议输出为xml,json和txt文件，excel文件在数据量较大（5万以上）会有性能问题。

###  这种图形化操作有什么优势？

效率！所见即所得！你可以试着用任意一种代码去写，烦死你

###  如何保存所有操作？

![image_1aiuctvts8qq1l7nn291lvk1ol841.png-33.7kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiuctvts8qq1l7nn291lvk1ol841.png-33.7kB.png)

会将所有刚才的操作保存在工程文件中。

###   我的服务器在Linux上，怎么办

Hawk是WPF,C#开发的，因此只能在Windows上运行，不过它生成的xml可以被Python解释，参考github上的etlpy.

###   Hawk是你一个人写的吗？用了多久

目前来看是这样的。业余时间四年

###  我想获取各个城市的，不限于美食的数据

这个就更复杂了，可以借助脚本实现，这是下一篇的话题。

##  总结

为了方便大家学习使用，刚才的整个操作已经上传到了Github。地址为https://github.com/ferventdesert/Hawk-Projects

大众点评-教程.xml

有任何问题，欢迎留言。




  [1]: http://static.zybuluo.com/buptzym/c9mq7m6kp5fi3icl59jsxheo/image_1airusse2977232s23148o1pi9.png
  [2]: http://static.zybuluo.com/buptzym/asybhai8k3hrs24pesdyi5od/image_1airv085o1pdf5u1egvuerjgnm.png
  [3]: http://static.zybuluo.com/buptzym/u0l98zjacgaz86umxgxyk7nm/image_1airv1slp1qnb1abtginug2kc913.png
  [4]: http://static.zybuluo.com/buptzym/7adqt2zzhq1zkj8htb67yqel/image_1airvo473v1h1rj61f3c1fvt9nq37.png
  [5]: http://static.zybuluo.com/buptzym/cy393cj7pte66uyc5s0ntg40/image_1airves3k7bs44uo0112o01l381t.png
  [6]: http://static.zybuluo.com/buptzym/vzp2wrgcqu84ywxzlmcr2xzt/image_1airvksaq19ac1ml84d92661rff2a.png
  [7]: http://static.zybuluo.com/buptzym/21o36ebagi5s6949d8qdkz22/image_1aiss9bm91n5fvcto85i7b1q8nm.png
  [8]: http://static.zybuluo.com/buptzym/c9mq7m6kp5fi3icl59jsxheo/image_1airusse2977232s23148o1pi9.png
  [9]: http://static.zybuluo.com/buptzym/oeo163r4iu5v0c2o8ek21ddo/image_1airvuu8ef54a78mmd7anvit4h.png
  [10]: http://static.zybuluo.com/buptzym/vy4zzvxfcpd9nvea2z5hj1lg/image_1ais0256617201p1d1ilv1na6rur4u.png
  [11]: http://static.zybuluo.com/buptzym/ednv2uc8wf9taxj4yp7a14i0/image_1ais08cp4dr31nmqr78u7u1lc85b.png
  [12]: http://static.zybuluo.com/buptzym/theesmj4b7mowdwfq55qm46h/image_1ais0br53kf7175t7du1pm210fo65.png
  [13]: http://static.zybuluo.com/buptzym/8u6elnu3hbj70gwpcsfgrrm8/image_1aissgnhl1ga3bm3m610g1uml13.png
  [14]: http://static.zybuluo.com/buptzym/h3gtaax8e550z03ellxngiyn/image_1aissqg3s1vp01pjg1rnibqjdgj1g.png
  [15]: http://static.zybuluo.com/buptzym/ft6vhudfo2yz4g766baakokh/image_1aiu7pia11jtv1fs1crf1eu41fg39.png
  [16]: http://static.zybuluo.com/buptzym/vsj3fs1p3hefbfu6egk6jyg8/image_1aiu81b8s1jic1eu3vnp1m5517i1m.png
  [17]: http://static.zybuluo.com/buptzym/uzqkq6976i38tceixeuphfex/image_1aiu84m21n836rr12jgqgt1b4b13.png
  [18]: http://static.zybuluo.com/buptzym/iaur01tzo0kljfh069ycmjfy/image_1aistmi1i1fibku21ni1loc66s2n.png
  [19]: http://static.zybuluo.com/buptzym/uak0916xglyhh8x0ieww9xob/image_1aistobto1jstlqfduiv7aihq34.png
  [20]: http://static.zybuluo.com/buptzym/3o4tzlp3t146se3qeetuq95w/image_1aistqm3s333b775vr1fsvif3h.png
  [21]: http://static.zybuluo.com/buptzym/lsb9lsmd7bdaymfmnz26kh42/image_1aistrkufs37jsm1o9c1opsgv53u.png
  [22]: http://static.zybuluo.com/buptzym/o8crnkobek9okgljh5p9wwam/image_1aisuaq7ib2k8blb5h3r51p5q4b.png
  [23]: http://static.zybuluo.com/buptzym/doo7bjnu1euea1n9r2cdqrhx/image_1aisui6polg49f134f1rvo8s94o.png
  [24]: http://static.zybuluo.com/buptzym/2rb3uo8kiaigy9sga7qv9xnv/image_1aisujo637p1iptteav57ct55.png
  [25]: http://static.zybuluo.com/buptzym/vvq250kd7mntb2p5hw8kaho5/image_1aiu937op1opn1u6a19tr1fppd8h19.png
  [26]: http://static.zybuluo.com/buptzym/cuuho5a8rfnwzaehnjyschdb/image_1aiu979h42tg9pg1uembp81fip1m.png
  [27]: http://static.zybuluo.com/buptzym/z9cnha1xpsmu97ftt5hgc8w6/image_1aisvmcfiep7vh6pn4msf1fhv5i.png
  [28]: http://static.zybuluo.com/buptzym/jst5l3etdj1z3ud4kau5sq3i/image_1aiu9bfftljg1lmi63h18d7r7823.png
  [29]: http://static.zybuluo.com/buptzym/xxgm3mgx17mnj43kw3ckubnt/image_1aiu9omeh1k1bf9pp1g10v51b3330.png
  [30]: http://static.zybuluo.com/buptzym/klrphq7fpp37iv4dtg3ryjco/image_1aiua4trfd641198v121hpt1cma3t.png
  [31]: http://static.zybuluo.com/buptzym/32a8im1rrdei6xbecdgbny7a/image_1aiua70q350rgop1tljiud1ovk4a.png
  [32]: http://static.zybuluo.com/buptzym/ixcz6pbctuga7q17lafgyoav/image_1aiua9pco128h1qsanbe1g3u12i54n.png
  [33]: http://static.zybuluo.com/buptzym/gg4gxxy0szga3cfb6jootbbz/image_1aiuaeshc14pib744751r5vfp554.png
  [34]: http://static.zybuluo.com/buptzym/hv45rid5loaoh7fulv899210/image_1aiuajl6nhdrdgh1t3sikl3a95h.png
  [35]: http://static.zybuluo.com/buptzym/mastmg94is32lrna5c0sbuzt/image_1aiub6nl319v01bo714su63p1gkq5u.png
  [36]: http://static.zybuluo.com/buptzym/grll4ghrd4lu7rlqcs1nrlpm/image_1aiubbcsg1qeg1l0712u216gu1me46b.png
  [37]: http://static.zybuluo.com/buptzym/xljzmvnxfag8yvjvfkr74fo5/image_1aiubggfv15qt1ot719b5d3l1de69.png
  [38]: http://static.zybuluo.com/buptzym/zr357ycxi3jebcgf0nv2b0yv/image_1aiublrdv1nio6a1l5k1vj5riom.png
  [39]: http://static.zybuluo.com/buptzym/5m5w6fk3kg0lsrpie549jxxf/image_1aiubrrau2rpdb61m7rcno1si113.png
  [40]: http://static.zybuluo.com/buptzym/7ghtgh8h1cgjrtv1j8fg0b45/image_1aiuc0p8pv4p18ks1rts1nfh1ivf1t.png
  [41]: http://static.zybuluo.com/buptzym/pskqcz337ersa52va02nn3n6/image_1aiuc41shkp2cju10rgrji1h942q.png
  [42]: http://static.zybuluo.com/buptzym/srbjv3cyo5mxgzh6us3kyw2y/image_1aiucj94dqir1rlt19n51n41ue63k.png
  [43]: http://static.zybuluo.com/buptzym/6g248woqyqk6h2ubondf2nco/image_1aiuctvts8qq1l7nn291lvk1ol841.png