# Hawk1 


不少朋友看了沙漠君的分析文章之后，都会询问我，那几十万条二手房，租房，薪酬，乃至天气数据，都是如何在十几分钟内采集到，而数据又是从哪里来的呢？
遇到这样的问题，我会回答，我用专门的工具，不用编程也能快速抓取。之后肯定又会问，在哪里能下载这个工具呢？我淡淡的说，我自己写的。。。
(这个B装的...我给95分！)

![image_1aiunn8pctfe1hp01dkua8q1tr99.png-469.8kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiunn8pctfe1hp01dkua8q1tr99.png-469.8kB.png)
沙漠君最近比较忙乱，说好的一大堆写作任务都还没有完成。授人以鱼不如授人以渔，我做了一个决定，将这套软件全部开源到GitHub！
从此以后，估计很多做爬虫的工程师要失业了。因为我的目标是让普通人也能使用，目标有点远大，不过貌似距离不远了。
这篇文章介绍爬虫大概的原理，文章最后会有程序地址和使用说明。

## 什么是爬虫
互联网是一张大网，采集数据的小程序可以形象地称之为爬虫或者蜘蛛。但这样的名字并不好听，所以我给软件**起名为Hawk，指代为"鹰"，能够精确,快速地捕捉猎物**。
爬虫的原理很简单，我们在访问网页时，会点击翻页按钮和超链接，浏览器会帮我们请求所有的资源和图片。所以，你可以设计一个程序，**能够模拟人在浏览器上的操作，让网站误认为爬虫是正常访问者，它就会把所需的数据返回回来**。
爬虫分为两种，一种是什么都抓的搜索引擎型爬虫，一般在百度(黑)这样的公司中使用。另一种就是沙漠君开发的，只精确地抓取所需的内容，比如我只要二手房信息，旁边的广告和新闻一律不要。
这套软件，可以基本不需编程，通过图形化的操作来快速设计爬虫，有点像Photoshop的意思。它能在20分钟内编写大众点评的爬虫（简化版只要3分钟），然后让它运行就好啦~
软件长这个样子，（高端黑高端黑）

![image_1aj0smtet1oi61idt1of317rn14tk10.png-415.2kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aj0smtet1oi61idt1of317rn14tk10.png-415.2kB.png)

## 自动将网页导出为Excel
那么，一个页面那么大，爬虫怎么知道我想要什么呢？

![image_1aiunq3dirbu1mh31ccm1l74ia1m.png-183.3kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiunq3dirbu1mh31ccm1l74ia1m.png-183.3kB.png)

人当然可以很容易地看出，上图的红框是二手房信息了，但机器不知道。网页是一种有结构的树，而重要信息所在的节点，往往枝繁叶茂。 举个不恰当的比方，一大家子人构成树状族谱，谁最厉害？当然是孩子多（能生），且孩子各个都很争气（孙子也很多），最好每个孩子都很像(N胞胎)的那个人，大家都会觉得他家太厉害了！

我们对整个树结构进行评分，自然就能找到那个最牛的节点，这个节点就是我们要的表格。

找到最牛爷爷之后，儿子们虽然相似，都有共性：个子高，长得帅，两条胳膊两条腿，但这些是普遍现象，没有信息量，我们关心的是特点。大儿子眼睛和其他人都不一样，那眼睛就是重要信息，三儿子最有钱，钱也是我们关心的。

因此，**对比儿子们的不同属性，我们就能知道哪些信息是重要的了**。

回到网页采集这个例子，通过一套有趣的算法，给一个网页的地址，软件就会自动地把它转成Excel!

（听不懂吧？听不懂正常， 不要在意这些细节！总之你知道这是沙漠君设计的就好了）

## 破解翻页限制

获取了一页的数据，这还不够，我们要获取所有页面的数据，这简单，我们让程序依次地请求第1页，第2页...数据就收集回来了。

就这么简单吗？网站怎么可能让自己宝贵的数据被这么轻松地抓走呢？所以它只能翻到第50页或第100页。链家就是这样:

![image_1aiupdcdrt2pmsf14bjk87abk9.png-5.1kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiupdcdrt2pmsf14bjk87abk9.png-5.1kB.png)

这也难不倒我们，每页有30个数据，所以100页最多能呈现3000个条数据。北京有16个区县，每个区县的小区数量肯定没有3000个，所以我们可以获取每个区县的所有小区的列表。每个小区的二手房再多也没有3000套（最多的小区可能有300多套在售二手房），这样就能获取链家的所有二手房了。

哈哈哈，是不是被沙漠君的机智所倾倒了？然后我们启动抓取器，Hawk就会给每个子线程（可以理解为机器人）分配任务：给我抓取这个小区的所有二手房！

然后你就会看到壮观的场面：一堆小机器人，同心协力地从网站上搬数据，超牛迅雷有没有？同时100个任务！上个厕所回来就抓完了。

![多任务执行](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/多任务执行.gif)

## 清洗：识别并转换内容

获取的数据大概长这样：

![image_1aiuq6o101sjl15as1nl9kh26ic1n.png-60.5kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aiuq6o101sjl15as1nl9kh26ic1n.png-60.5kB.png)

但你会看到，里面会有些奇怪的字符应该去去掉。xx平米应该都把数字提取出来。而售价，有的是373万元，有的是2130000元，这些都很难处理。

没关系！Hawk能够自动识别所有的数据：
 
 - 发现面积那一列的乱码，自动去掉
 - 识别价格，并把所有的价格都转换为万元单位
 - 发现美元，转换为人民币
 - 发现日期，比如2014.12或2014年12.31，都能转换为2014年12月31日
 
哈哈，然后你就能够轻松地把这些数据拿去作分析了，纯净无污染！

## 破解需要登录的网站

此处的意思当然不是去破解用户名密码，沙漠君还没强到那个程度。

有些网站的数据，都需要登录才能访问。这也难不倒我们。

当你开启了Hawk内置了嗅探功能时，Hawk就像一个录音机一样，会记录你对目标网站的访问操作。之后它就会按需要将其重放出来，从而实现自动登录。

你会不会担心Hawk保存你的用户名密码？不保存怎么自动登录呢？但是Hawk是开源的，所有代码都经过了审查，是安全的。你的私密信息，只会躺在你自己的硬盘里。

![简单的自动嗅探]](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/简单的自动嗅探.png)

(我们就这样自动登录了大众点评)

## 是不是我也可以抓数据了？

理论上是的，但道高一尺魔高一丈，不同的网站千差万别，对抗爬虫的技术也有很多种。而且爬虫对细节非常敏感，只要错一点，后面的步骤就可能进行不下去了。

怎么办呢？沙漠君把之前的操作保存并分享出来，你只要加载这些文件就能快速获取数据了。

如果你有其他网站的获取需求，可以去找你身边的程序员同学，让他们来帮忙抓数据，或让他们来试试Hawk，看看谁的效率更高。

如果你是文科生或者是妹子，那还是建议你多看看东野奎吾和村上春树，直接上手这么复杂的软件会让你抓狂的（已经有很多血淋淋的案例了）。

## 在哪里获取软件和教程？

软件的教程和下载链接，可参考沙漠君的技术博客，在百度(黑)上面搜索“沙漠之鹰 博客园”，即可：
![image_1aj0t276v15m6pd6eme1un815ia1d.png-170.1kB](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/image_1aj0t276v15m6pd6eme1un815ia1d.png-170.1kB.png)

第二个就是。最新版本已经公布在百度网盘里了。

  [1]: http://static.zybuluo.com/buptzym/mlukqqw44zqrtkd3m4honh6m/image_1aiunn8pctfe1hp01dkua8q1tr99.png
  [2]: http://static.zybuluo.com/buptzym/hrbn9acls86l21p8ppnlmfrr/image_1aj0smtet1oi61idt1of317rn14tk10.png
  [3]: http://static.zybuluo.com/buptzym/jsso9y452ugut3yaqgkgq9jx/image_1aiunq3dirbu1mh31ccm1l74ia1m.png
  [4]: http://static.zybuluo.com/buptzym/kx7jxdbyit5lczr113u8px9b/image_1aiupdcdrt2pmsf14bjk87abk9.png
  [5]: http://static.zybuluo.com/buptzym/qkl0vavjn6cj007qfk2k3gqg/1.gif
  [6]: http://static.zybuluo.com/buptzym/u6ogkqjt3adusen8w0gh2h2y/image_1aiuq6o101sjl15as1nl9kh26ic1n.png
  [7]: http://static.zybuluo.com/buptzym/7adqt2zzhq1zkj8htb67yqel/image_1airvo473v1h1rj61f3c1fvt9nq37.png
  [8]: http://static.zybuluo.com/buptzym/aj3i2g5tte7jhofq3btu4bql/image_1aj0t276v15m6pd6eme1un815ia1d.png