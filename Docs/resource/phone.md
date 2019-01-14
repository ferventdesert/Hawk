# Hawk的Https和手机抓取教程

Hawk5新增了对手机app抓取的功能！其中，浏览器，链家，微信公众号，朋友圈经测试都可以抓取，但对于部分加密应用，如医院挂号等应用还不支持，原因待查。

https://www.cr173.com/html/20064_1.html

在PC端进行嗅探时，我们通常在获取要抓取的目标请求后，就不需要继续嗅探了，因此可选择“自动启停”。

但在抓取手机数据时，若不打开代理，则完全无法上网。因此建议关闭自动启停功能。

## HTTPS抓取

Hawk本身已经支持了HTTPS抓取，但是部分网站会要求检查证书，由于Hawk底层使用的是fiddler作为嗅探内核，当访问Https无法抓取时，可参考以下帖子的内容：
- https://www.cnblogs.com/lelexiong/p/9054626.html
- https://blog.csdn.net/jq656021898/article/details/72876731

