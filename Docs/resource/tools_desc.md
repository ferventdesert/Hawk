# 转换器
## 搜索位置(BaiduLocation)

通过百度API获取当前地标的经纬度坐标，需要拖入代表地名的列
在Hawk 3之后的版本，需要在百度地图API中注册账户，并将token填入配置中，参考:

> http://lbsyun.baidu.com/index.php?title=webapi

### 所属地市(Region):
* 类型:String 默认值:北京  
* 通过城市名称进行信息检索
### 标签(Tag):
* 类型:String 
* 如医院，美食等
***
## 获取IP的坐标(GetIPLocation)
获取某一ip地址的经纬度坐标
***
## 获取路径信息(GetRoute)
从当前地名，运动到对应坐标所需的时间
### 目标位置(Dest):
* 类型:String 
* 通过城市名称进行信息检索
### 源城市(SourceCity):
* 类型:String 默认值:北京  
* 通过城市名称进行信息检索
### 目标城市(DestCity):
* 类型:String 默认值:北京  
* 通过城市名称进行信息检索
### 运动方案(ModeSelector):
* 类型:string选项 默认值:公交  
* 没有描述
***
## 检索附近(NearbySearch)
获取当前经纬度某一半径范围内的所有地物，需要拖入的为代表经度的列
### 查询地物，如`医院`,`商场等`(Query):
* 类型:String 
* 如公园，车站等
### 纬度列(Lng):
* 类型:String 默认值:pos_lng  
* 代表纬度所在的列
### 搜索半径(Radius):
* 类型:Int32 默认值:2000  
* 没有描述
### 所有结果(AllResult):
* 类型:Boolean 默认值:False  
* 没有描述
***
## 自然语言处理(NlpTF)
通过语言云获取的NlpTF功能，包括分词，词性标注，主题提取等
### ResultType(ResultType):
* 类型:ContentType 默认值:Text  
* 没有描述
### Pattern(Pattern):
* 类型:Pattern 默认值:分词  
* 没有描述
***
## 语言翻译转换(TransTF)
从当前语言翻译为目标语言(调用百度API)
### 应用中心账号(ClientID):
* 类型:String 
* 没有描述
### key(Key):
* 类型:String 
* 没有描述
### Source(Source):
* 类型:string选项 默认值:自动检测  
* 没有描述
### Target(Target):
* 类型:string选项 默认值:自动检测  
* 没有描述
***
## 添加新列(AddNewTF)
为数据集添加新的列，值为某固定值
### 生成值(NewValue):
* 类型:String 
* 没有描述
***
## 自增键生成(AutoIndexTF)
自动生成一个从起始索引开始的自增新列
### 起始索引(StartIndex):
* 类型:Int32 默认值:0  
* 没有描述
***
## 列名修改器(RenameTF)

对列名进行修改,常用

拖入的列是要修改的列，填写`输出列`后，原始列被删除，内容转移到新列上

除了手工拖入模块，也可直接在数据清洗列上的文本框中直接修改列名，按回撤提交，可达到同样效果。

***
## 删除该列(DeleteTF)

删除所在列的内容

删除之后，在该列的内容和所有工具就不再可见。要想修改可以在数据清洗界面左侧的模块列表里选择，修改和删除。

***
## 数据库匹配(JoinDBTF)
用于完成与数据库的join操作和匹配，目前测试不完善
### 查询多数据(IsMutliDatas):
* 类型:Boolean 默认值:False  
* 启用该项时，会查询多个满足条件的项，同时将同一列保存为数组
### 匹配方式(SearchStrategy):
* 类型:DBSearchStrategy 默认值:Contains  
* 字符串匹配，如like,contains等，符合sql标准语法
### 表主键(KeyName):
* 类型:String 
* 字符串匹配，如like,contains等，符合sql标准语法
***
## 重复当前值(RepeatTF)
对当前行进行重复性生成
### 重复模式(RepeatType):
* 类型:RepeatType 默认值:OneRepeat  
* 没有描述
### 重复次数(RepeatCount):
* 类型:String 默认值:1  
* 没有描述
***
## 获取请求响应(ResponseTF)
使用 网页采集器 获取网页数据，得到响应字段的值并添加到对应的属性中
### 爬虫选择(CrawlerSelector):
* 类型:可编辑选项 
* 填写采集器或模块的名称
### 响应头(HeaderFilter):
* 类型:String 
* 要获取的响应头的名称，多个之间用空格分割，不区分大小写
***
## 重试补数据(SupplierTF)
尝试对已有错误日志重跑补数据，错误日志需包含错误所在的任务名和模块名
### 使用原执行器(InnerExecute):
* 类型:Boolean 默认值:False  
* 没有描述
***
## 时间转字符串(Time2StrTF)
将时间转换为特定格式的字符串
### 转换格式(Format):
* 类型:String 默认值:yyyy-MM-dd  
* 没有描述
***
## URL字符转义(UrlTF)
对超链接url生成URL编码后的字符串，用以进行远程访问
### 转换选项(ConvertType):
* 类型:ConvertType 默认值:Decode  
* 没有描述
***
## HTML字符转义(HtmlTF)

删除HTML标签和转义符号

当页面包含HTML时，一些字符可能已经被转义了，例如空格成了`nsbp%`。拖入到对应的列，即可将转义符号恢复为之前的表示

注意:

- Hawk的Web访问器比python更加智能，默认对带特殊符号和中文的URL进行编码，所以这个模块用的并不多。

### 转换选项(ConvertType):
* 类型:ConvertType 默认值:Decode  
* 没有描述
***
## 正则分割(RegexSplitTF)
使用正则表达式分割字符串
### 倒序(FromBack):
* 类型:Boolean 默认值:False  
* 勾选此项后，选择从后数的第n项
### 工作模式(IsManyData):
* 类型:ScriptWorkMode 默认值:One  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



### 匹配编号(Index):
* 类型:Int32 默认值:0  
* 当值小于0时，可同时匹配多个值
### 表达式(Script):
* 类型:String 
* 没有描述
***
## 合并多列(MergeTF)

该模块可以将多个列合并成一个列

常见的如将page合并到url中，也可以通过文件名，合并出要保存的文件的位置，是使用次数最多的模块。

它的操作非常灵活，例如格式为： `format= {0}+{1}+{2}` ， 其他列为`B C`，则代表将输入列，B列和C列的内容直接拼接。

- `{0}`：输入列,
- `{1}`：`其他列`中的第0项，`{1}`代表第1项
- `[a]`：A列中的内容
- `{config}` : 工程全局配置中键为config的值

总结来说： 方括号代表从本行的其他列，大括号可从全局配置中读取内容
若全局配置或数据中不包含对应的内容，则该列自动为空

### 其他项(MergeWith):
* 类型:String 
* 写入多个列名，中间使用空格分割，若合并输入列，则可以为空
### 格式(Format):
* 类型:String 
* 形如'http:\\{0}:{1},{2}...' - 输入列的序号为0， - 之后的1,2分别代表【其他项】的第0和第1个值
### 参考格式(ReferFormat):
* 类型:string选项 
* 为了方便用户，下拉菜单中提供了已有 网页采集器 配置的url，可修改后使用
***
## Python转换器(PythonTF)

执行特定的python代码或脚本，最后一行需要为值类型，作为该列的返回值
例如，有两列a和b, 要将它们按字符串相加:

`a+b`
若希望按数值类型相加， 则需要提前将其转换

`float(a)+float(b)`
也可以提前定义函数:

```
def add(x,y):
return float(x)+float(y)
add(a,b)
```
也可以使用lambda:

``` 
f= lambda a,b: a+b
f(a,b)
```

### 注意：

1. 你可以在文本框中定义函数，但不建议太过复杂
2. 很难引入第三方库，这受限于C#使用的ironpython(一个C#和Python交互的模块)的功能, Hawk3中引入了调用第三方库的功能，通过编写库路径，从而能够在脚本中import库，但功能支持并不好。
3. 不论操作如何，脚本的最后一行需要是个可求值的元素，传递给对应的列，比如
- `return a` #这是错误的
- `a+b` 正确，可求值
- `lambda x:x+1`  你确定要返回一个函数或lambda?肯定也是不对的
4. Hawk并不预定义每个列具体的类型，因此需要在Python代码中对其进行类型和是否为空的判断。

### 工作模式(ScriptWorkMode):
* 类型:ScriptWorkMode 默认值:NoTransform  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



### 执行脚本(Script):
* 类型:String 默认值:value  
* 没有描述
### Python库路径(LibraryPath):
* 类型:String 
* 若需要引用第三方Python库，则可指定库的路径，一行一条
***
## 正则替换(ReReplaceTF)
通过正则表达式替换数值
### 替换为(ReplaceText):
* 类型:String 
* 没有描述
### 工作模式(IsManyData):
* 类型:ScriptWorkMode 默认值:One  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



### 匹配编号(Index):
* 类型:Int32 默认值:0  
* 当值小于0时，可同时匹配多个值
### 表达式(Script):
* 类型:String 
* 没有描述
***
## 正则转换器(RegexTF)

通过正则表达式提取内容, 可匹配一个和多个内容
设置匹配编号为正数n时，它可将第n个匹配结果转换到新列上。如果不填写新列名，则内容直接覆盖原始列。
输入负数n时，则会返回倒数第n个内容。如果没有发现匹配，则返回空

### 工作模式(IsManyData):
* 类型:ScriptWorkMode 默认值:One  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



### 匹配编号(Index):
* 类型:Int32 默认值:0  
* 当值小于0时，可同时匹配多个值
### 表达式(Script):
* 类型:String 
* 没有描述
***
## 提取数字(NumberTF)

提取当前列中出现的数值

它是正则转换器的特例，它能够提取浮点或整数，也能包含正负数

### 工作模式(IsManyData):
* 类型:ScriptWorkMode 默认值:One  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



### 匹配编号(Index):
* 类型:Int32 默认值:0  
* 当值小于0时，可同时匹配多个值
### 表达式(Script):
* 类型:String 默认值:(-?\d+)(\.\d+)?  
* 没有描述
***
## 字符首尾抽取(StrExtractTF)

提取字符串中，从首串到尾串中间的文本内容

当文本为`CABD`时，需要获取B，而B非常长，写正则表达式提取有很大困难时，可以使用本模块。此时，首串填写A，尾串填写D，则Hawk就能将B提取出来。如果勾选`包含首尾字符`，则输出`ABD`，否则只有B。注意:

- 建议A和D在文本中是唯一的，否则抽取出来的B可能并不是你想要的。
- 该工具特别适合在抽取网页的某一特定内容时使用.

### 首串(Former):
* 类型:String 
* 没有描述
### 尾串(End):
* 类型:String 
* 没有描述
### 包含首尾串(HaveStartEnd):
* 类型:Boolean 默认值:False  
* 返回的结果里是否包含首串和尾串
***
## 启动并行(ToListTF)

可设置任务并行方式和参数

该模块在执行时，会切分本模块前后的数据流，以前侧的数据为种子，后侧的任务为mapper执行.

子线程名称和子线程数量，都支持直接写值，或使用方括号表达式来获取别的列的内容。

例如，如果你确定每个子任务都会获取100条数据，就可以在`子线程数量`中填写`100`，之后当该任务获取了50个元素时，进度条正好处在50%的位置。如果有一列名为“小区名”， 则可以在`子线程名称`栏目中填写`[小区名]` ，Hawk就会把小区名列中的内容作为子任务的名称。

注意:

1. 该转换器在调试和串行执行模式不起任何作用，仅仅作为一个标志
2. 它能够在并行模式下，给执行引擎一个并行分叉的标志。




### 子线程数量(MountColumn):
* 类型:String 
* 每个子线程将要获取的数量，用于显示进度条，可不填
### 分组并行数量(GroupMount):
* 类型:Int32 默认值:1  
* 将多个种子合并为一个任务执行，这对于小型种子任务可有效提升效率
### 显示独立任务(DisplayProgress):
* 类型:Boolean 默认值:True  
* 是否将每个子线程插入到任务队列中，从而显示进度
***
## 清除空白符(TrimTF)

清除字符串前后和中间的空白符
默认能去掉文本前后的空白字符，也可以通过勾选内部选项，清除文本中间的空白符
注意：

- 使用`正则替换`也能实现类似的要求，只是本模块会更简单。

### 清除中间空格(ReplaceBlank):
* 类型:Boolean 默认值:False  
* 没有描述
### 空白符替换为空格(ReplaceInnerBlank):
* 类型:Boolean 默认值:True  
* 没有描述
***
## 路径是否存在(FileExistFT)
判断某一个文件是否已经在指定路径上
***
## 重复项合并(MergeRepeatTF)
对重复的数据行，进行合并操作
### 延迟输出(IsLazyLinq):
* 类型:Boolean 默认值:False  
* 不勾选此选项使用枚举式迭代，需保证在本模块之后没有其他操作，否则请勾选该选项
### 合并到集合的属性(CollectionColumns):
* 类型:String 
* 填入空格分割的列名，对本模块所在列的值相同的所有属性分别进行纵向合并数组
### 求和属性(SumColumns):
* 类型:String 
* 可填入空格分割的多个列名 对本模块所在列的值相同的所有属性，分别进行按列求和
***
## 矩阵转置(DictTF)
将列数据转换为行数据，拖入的列为key
***
## 子任务-转换(EtlTF)
调用所选的子任务作为转换器，有关子任务，请参考相关文档
### 递归到下列(IsCycle):
* 类型:Boolean 默认值:False  
* 没有描述
### 工作模式(IsManyData):
* 类型:ScriptWorkMode 默认值:List  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



### 子任务-选择(ETLSelector):
* 类型:可编辑选项 
* 输入或选择调用的子任务的名称
### 调用范围(ETLRange):
* 类型:String 
* 设定调用子任务的模块范围，例如2:30表示被调用任务的第2个到第30个子模块将会启用，其他模块忽略，2:-1表示从第2个到倒数第二个启用，符合python的slice语法，为空则默认全部调用
### 属性映射(MappingSet):
* 类型:String 
* 源属性:目标属性列 多个映射中间用空格分割，例如A:B C:D, 表示主任务中的A,B属性列会以C,D的名称传递到子任务中
***
## XPath转换器(XPathTF)

通过XPath或CSS选取html中的子节点文档

当输入的单元格内容为html文档，而又想提取其部分数据，用 网页采集器 又`杀鸡用牛刀`，则可以考虑使用它。

### XPath
关于XPath语法，可参考[教程](http://www.w3school.com.cn/xpath/xpath_syntax.asp)

XPath可以非常灵活，例如：

- bookstore	选取 bookstore 元素的所有子节点。
- /bookstore	选取根元素 bookstore。注释：假如路径起始于正斜杠( / )，则此路径始终代表到某元素的绝对路径！
- bookstore/book	选取属于 bookstore 的子元素的所有 book 元素。
- //book	选取所有 book 子元素，而不管它们在文档中的位置。
- bookstore//book	选择属于 bookstore 元素的后代的所有 book 元素，而不管它们位于 bookstore 之下的什么位置。
- //@lang	选取名为 lang 的所有属性。
- //@src 可匹配所有src标签
- //title[@lang] 选取所有拥有名为 lang 的属性的 title 元素
还可以通过`|`对多个表达式进行混合，Hawk支持了完整的XPath语法，因此不论是` 网页采集器 `以及数据清洗的`XPath`转换器，都能极其灵活地实现各种需求。



### CSSSelector
多数情况下，使用XPath就能解决问题，但是CSSSelector更简洁，且鲁棒性更强。关于它的介绍，可[参考教程](http://www.w3school.com.cn/cssref/css_selectors.asp)
当然，大部分情况不需要那么复杂，只要记住以下几点：

- `.name` 获取所有id为name的元素
- `#name` 获取所有class为name的元素
- `p` 获取所有p元素
- `ul > li` 获取所有父节点是ul的li元素


### 路径(XPath):
* 类型:String 
* 没有描述
### 工作模式(IsManyData):
* 类型:ScriptWorkMode 默认值:One  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



### 获取正文(GetText):
* 类型:Boolean 默认值:False  
* 勾选此项后，会智能提取新闻正文，XPath路径可为空
### 选择器(SelectorFormat):
* 类型:SelectorFormat 默认值:XPath  
* 
### 抓取目标(CrawlType):
* 类型:CrawlType 默认值:InnerText  
* 
***
## 门类枚举(XPathTF2)
要拖入HTML文本列,可将页面中的门类，用Cross模式组合起来，适合于爬虫无法抓取全部页面，但可以按分类抓取的情况。需调用 网页采集器 ，具体参考文档-XPathTF2
### 爬虫选择(CrawlerSelector):
* 类型:可编辑选项 
* 填写采集器或模块的名称
***
## 字符串分割(SplitTF)
通过字符分割字符串
### 按字符直接分割(ShouldSplitChars):
* 类型:Boolean 默认值:False  
* 将原文本每个字符直接分割开
### 空格分割(SplitPause):
* 类型:Boolean 默认值:False  
* 没有描述
### 匹配编号(Index):
* 类型:String 默认值:0  
* 
- 若想获取分割后的第0个元素，则填入0，获取倒数第一个元素，则填入-1
- 可输入多个匹配编号，中间以空格分割，
- 【输出列】也需要与之一对应

### 分割字符(SplitChar):
* 类型:String 
* 多个分隔符用空格分割，换行符用\\t，制表符用\\t
***
# 常用
## 从爬虫转换(CrawlerTF)

使用 网页采集器 获取网页数据，拖入的列需要为超链接


### 一般的get请求

一般情况下, 将从爬虫转换拖入到对应的URL列中，通过下拉菜单选择要调用的爬虫名称，即可完成所有的配置：

![请求设置](https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs/请求设置.png)

本模块是沟通网页采集器和数据清洗的桥梁。本质上说，网页采集器是针对获取网页而特别定制的`数据清洗模块`。

你需要填写`爬虫选择`，告诉它要调用哪个采集器。注意：

- 早期版本的Hawk,会默认选择在`算法模块`的第一个网页采集器，但实践证明这样会导致问题，后来就取消了功能。



### 实现post请求

web请求中，有两种主要的请求类型:post和get。 使用POST能支持传输更多的数据。更多的细节，可以参考http协议的相关文档，网上汗牛充栋，这里就不多说了。

post请求时，Hawk要给服务器需要传递两个参数：url 和post。一般来说，在执行post请求时，url是稳定的，post值是动态改变的。

首先要配置调用的网页采集器为`post`模式（打开网页采集器，请求详情，模式->下拉菜单）。

之后，需要将`从爬虫转换`拖到要调用的url列上。如果没有url列，可以通过`添加新列`，生成要访问的url列。

之后，我们要将post数据传递到网页采集器中。你总是可以通过`合并多列`拼接或各种手段，生成要Post的数据列。之后，可以在`从爬虫转换`中的`post数据`中，填写`[post列]`， 而`post列`就是包含post数据的列名。 注意：

- Hawk使用方括号语法，来引用其他列的值作为当前的参数


### Post数据(PostData):
* 类型:String 
* 没有描述
### 代理配置(Proxy):
* 类型:String 
* 没有描述
### 爬虫选择(CrawlerSelector):
* 类型:可编辑选项 
* 填写采集器或模块的名称
***
## 分页(SplitPageTF)

根据总页数和每页数量进行分页操作，拖入列为总页数。 相比于使用Python转换器，可极大地简化操作。注意：

- 早期版本的Hawk中，若希望对网页进行分页，需要拖入多个模块才能实现，非常繁琐。
- 本模块在输入数量数，每页数量和起始值之后，即可自动创建步进整数。
- 例如总数量270, 每页数量为20，起始值为1，则生成的列为1，2，3..14

### 最小值(MinValue):
* 类型:String 默认值:1  
* 除了直接填写数值，还可通过方括号表达式从其他列传入
### 每页数量(ItemPerPage):
* 类型:String 默认值:1  
* 除了直接填写数值，还可通过方括号表达式从其他列传入
***
## 转换为Json(JsonTF)

从字符串转换为json（数组或字典类型）

当输入字符串是Json时，可以通过Json转换器将文本转换为Json。其工作模式和Python转换器一样，此处不赘述。

json转换器的转换结果，实际上是一个动态类型的python对象。例如如下json:
```
{
'key':[{}{}{}]
'value':
{
'key1':value
'key2':value
}
}
```
拖入json转换器到该列，如果工作模式是`不进行转换`，则你可以在转换结果列，拖入`Python转换器`,脚本内容填写`data[key]`,工作模式选择`转换为列表`，则key中的数组自动会被提取出来。

注意事项:

1. python和json转换器配合使用，能够解决一大类ajax网页的问题。更详细的内容，可参考
2. 网页的json格式并不标准，此时需要通过其他工具，对字符串进行预处理，方可转换为json。
3. 如果json非常复杂，是不建议直接用Hawk做数据清洗的，正确的做法是将json保存成文本，之后用其他工具或手工编写代码后处理。

### 工作模式(ScriptWorkMode):
* 类型:ScriptWorkMode 默认值:NoTransform  
* 
### 多文档


生成多条数据（文档）


### 单文档

单文档

### 不进行转换



***
## 延时(DelayTF)

在工作流中插入延时，可休眠固定长度避免爬虫被封禁，单位为ms

在不同的位置插入延时有不同的行为，例如在模块A之前插入延时，则A模块每次执行前都会延时固定长度。
除了拖入延时，在`串行模式`下填入延时时间，则会在每个web请求前插入指定的延时，更加方便。

### 延时值(DelayTime):
* 类型:String 
* 单位为毫秒，也可使用方括号语法，例如[a]表示从a列中读取延时长度
***
## 子任务-执行(EtlEX)

调用其他任务，作为执行器，一般位于任务的末尾。

子任务是Hawk中高级但却非常重要的功能，可以实现例如多级跳转，采集详情页等等的功能，非常强大。
所谓`子任务`，就是能先构造出一个任务，然后被其他任务调用。被调用的任务就是子任务。我们应该能够了解子任务其实就是函数，可以定义输入列和输出列，把整个子任务看成一个模块，从而方便重用。

使用子任务-执行的例子： 先设计构造获取某个页面全部图片的任务， 并创建主任务，在主任务中调用刚才创建的子任务。

### 添加到任务(AddTask):
* 类型:Boolean 默认值:False  
* 勾选后，本子任务会添加到任务管理器中
### 子任务-选择(ETLSelector):
* 类型:可编辑选项 
* 输入或选择调用的子任务的名称
### 调用范围(ETLRange):
* 类型:String 
* 设定调用子任务的模块范围，例如2:30表示被调用任务的第2个到第30个子模块将会启用，其他模块忽略，2:-1表示从第2个到倒数第二个启用，符合python的slice语法，为空则默认全部调用
### 属性映射(MappingSet):
* 类型:String 
* 源属性:目标属性列 多个映射中间用空格分割，例如A:B C:D, 表示主任务中的A,B属性列会以C,D的名称传递到子任务中
***
# 过滤器
## 空对象过滤器(NullFT)

检查文本是否为空白符或null，常用

可以过滤掉所有内容为空，或字符串全部都是空字符的情况

### 求反(Revert):
* 类型:Boolean 默认值:False  
* 将结果取反后返回
例如筛选器判断为`正确`，则返回错误

### 调试时启用(IsDebugFilter):
* 类型:Boolean 默认值:True  
* 没有描述
### 过滤模式(FilterWorkMode):
* 类型:FilterWorkMode 默认值:ByItem  
* 没有描述
***
## 数值范围过滤器(RangeFT)

从数值列中筛选出从最小值到最大值范围的文档

可以填写最大值和最小值，只有本列的值处在该范围内的文档可被留下。若该单元格的内容不是数字，则会被忽略。

### 最大值(Max):
* 类型:String 
* 没有描述
### 最小值(Min):
* 类型:String 
* 没有描述
### 求反(Revert):
* 类型:Boolean 默认值:False  
* 将结果取反后返回
例如筛选器判断为`正确`，则返回错误

### 调试时启用(IsDebugFilter):
* 类型:Boolean 默认值:True  
* 没有描述
### 过滤模式(FilterWorkMode):
* 类型:FilterWorkMode 默认值:ByItem  
* 没有描述
***
## 正则筛选器(RegexFT)

编写正则表达式来过滤文本

需要列名，输入正则表达式，和其最小匹配的内容数量，即可过滤内容。
有关正则表达式，可参考[这里](https://www.jb51.net/tools/zhengze.html)

### 表达式(Script):
* 类型:String 
* 没有描述
### 最小匹配数(Count):
* 类型:Int32 默认值:1  
* 只有正则表达式匹配该文本的结果数量大于等于该值时，才会保留，默认为1
### 求反(Revert):
* 类型:Boolean 默认值:False  
* 将结果取反后返回
例如筛选器判断为`正确`，则返回错误

### 调试时启用(IsDebugFilter):
* 类型:Boolean 默认值:True  
* 没有描述
### 过滤模式(FilterWorkMode):
* 类型:FilterWorkMode 默认值:ByItem  
* 没有描述
***
## 删除重复项(RepeatFT)
以拖入的列为唯一主键，按行进行去重，仅保留重复出现的第一项
### 求反(Revert):
* 类型:Boolean 默认值:False  
* 将结果取反后返回
例如筛选器判断为`正确`，则返回错误

### 调试时启用(IsDebugFilter):
* 类型:Boolean 默认值:True  
* 没有描述
### 过滤模式(FilterWorkMode):
* 类型:FilterWorkMode 默认值:ByItem  
* 没有描述
***
## 数量范围选择(NumRangeFT)

选择一定数量的行，如跳过前100行，再选取50条

不需要列名，它可以跳过并选择部分文档，类似于sql语句中的skip和limit关键字。
注意:

- 当skip数量过大，而目标数据是延迟执行时，skip会需要相当长的时间，而任务进度条没有任何反应，因此尽量避免这种设计

### 跳过(Skip):
* 类型:Int32 默认值:0  
* 没有描述
### 获取(Take):
* 类型:Int32 默认值:0  
* 没有描述
### 求反(Revert):
* 类型:Boolean 默认值:False  
* 将结果取反后返回
例如筛选器判断为`正确`，则返回错误

### 调试时启用(IsDebugFilter):
* 类型:Boolean 默认值:True  
* 没有描述
### 过滤模式(FilterWorkMode):
* 类型:FilterWorkMode 默认值:ByItem  
* 没有描述
***
# 执行器
## 写入数据库(DbEX)
进行数据库操作，包括写入，删除和更新，输入列为表的主键
### 操作类型(ExecuteType):
* 类型:EntityExecuteType 默认值:OnlyInsert  
* 选择数据库的操作，如插入，删除，更新等
### 表名(TableNames):
* 类型:可编辑选项 
* 
必填，若数据库不存在该表，则会根据第一条数据的列自动创建表
不符合数据库要求的列名会被替换

***
## 保存超链接文件(SaveFileEX)

保存对应链接的文件，如图片，视频等
拖入的列为文件的超链接地址
`保存位置`:可以使用方括号表达式，将某一列的内容传递过来
注意:

- 一些网站必须要求登录以后才能下载内容。而如果你已经配置好能正常访问该网站的` 网页采集器 `，那么就可以在`共用采集器名`中填写这个采集器的名称，此时本模块会使用那个采集器的header进行抓取。

### 保存位置(SavePath):
* 类型:String 
* 
路径或文件名，例如D:\\file.txt, 可通过'[]'引用其他列，
若为目录名，必须显式以/结束，文件名将会通过url自动解析

### 爬虫选择(CrawlerSelector):
* 类型:可编辑选项 
* 填写采集器或模块的名称
### 是否异步(IsAsync):
* 类型:Boolean 默认值:False  
* 没有描述
***
## 写入数据表(TableEX)
将数据保存至软件的数据管理器中，之后可方便进行其他处理，拖入到任意一列皆可，常用
### 表名(Table):
* 类型:String 
* 没有描述
***
## 写入文件文本(WriteFileTextTF)
写入文件中的文本，由于在并行模式下同时写入文件可能会导致问题，因此尽量使用串行模式
### 路径(FileName):
* 类型:String 
* 例如d:\\test\\mydb.sqlite
### 编码(EncodingType):
* 类型:EncodingType 默认值:UTF8  
* 没有描述
***
# 生成器
## 生成区间时间(DateRangeGE)
生成某范围内的日期和时间
### 最小值(MinValue):
* 类型:String 默认值:2018-10-08 22:25:41:6985  
* 按类似yyyy-MM-dd HH:mm:ss:ffff格式进行填写
### 最大值(MaxValue):
* 类型:String 默认值:2018-10-11 22:25:41:6985  
* 按类似yyyy-MM-dd HH:mm:ss:ffff格式进行填写
### 间隔(Interval):
* 类型:String 默认值:1h 0m 0s  
* 按类似1'h '3'm '5's'格式进行填写
### 生成时间格式(Format):
* 类型:String 默认值:yyyy-MM-dd HH:mm:ss:ffff  
* 
可参考C# DateTime Format相关方法，以下是一些例子：

- yyyy-MM-dd等
- yyyy-MM

### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 读取文件文本(ReadFileTextGE)

获取文件中的全部纯文本内容

注意与【读取文件数据】区别，后者为一行一条数据，前者则将所有文本（包括换行符）都看为一条数据

### 路径(FileName):
* 类型:String 
* 例如d:\\test\\mydb.sqlite
### 编码(EncodingType):
* 类型:EncodingType 默认值:UTF8  
* 没有描述
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 读取文件数据(ReadFileGe)

从文件中读取数据内容，为了保证正确读取，需配置文件格式和读取属性
除了一般的数据库导入导出，Hawk还支持从文件导入，支持的文件类型包括：

- Excel
- CSV(逗号分割文本文件)
- TXT (制表符分割文本文件)
- Json
- xml

### 路径(FileName):
* 类型:String 
* 例如d:\\test\\mydb.sqlite
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 获取文件夹文件(FolderGE)

获取文件夹下的所有文件，拖入列为文件夹的名称

可直接对文件名的筛选

### 路径(FolderPath):
* 类型:String 
* 没有描述
### 筛选模式(Pattern):
* 类型:String 默认值:*.*  
* 符合windows的文件通配符筛选规范
### 是否递归(SearchOption):
* 类型:SearchOption 默认值:TopDirectoryOnly  
* 即是否获取子文件夹的子文件
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 从数据库生成(DbGE)
从数据库读取内容，需提前在`数据视图`中新建或配置连接
### 2.操作表名(TableNames):
* 类型:string选项 
* 没有描述
### 3.数量(Mount):
* 类型:Int32 默认值:-1  
* 没有描述
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 生成随机数(RandomGE)
生成某范围内和指定数量的随机数
### 最小值(MinValue):
* 类型:String 默认值:1  
* 没有描述
### 最大值(MaxValue):
* 类型:String 默认值:100  
* 没有描述
### 数量(Count):
* 类型:String 默认值:100  
* 没有描述
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 生成区间数(RangeGE)

生成某范围内的数值
例如生成从0到100，步进为1的值即为1,2,3..100

### 最小值(MinValue):
* 类型:String 默认值:1  
* 没有描述
### 最大值(MaxValue):
* 类型:String 默认值:1  
* 除了填写数字，还可以用方括号表达式，如[a]表示从a列获取值作为本参数的真实值
### 间隔(Interval):
* 类型:String 默认值:1  
* 如需生成数组1,3,5,7,9，则间隔为2
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 从数据表生成(TableGE)
从数据管理中已有的数据表中生成，常用
### 数据表(TableSelector):
* 类型:string选项 
* 选择所要连接的数据表
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 从文本生成(TextGE)
每行一条数据，常用
### 文本(Content):
* 类型:String 
* 每行一条
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 请求队列(BfsGE)

### BFS起始位置(StartURL):
* 类型:String 
* 没有描述
### 延时时间(DelayTime):
* 类型:Int32 默认值:0  
* 没有描述
### 工作模式(MergeType):
* 类型:MergeType 默认值:Cross  
* 没有描述
***
## 子任务-生成(EtlGE)
调用其他任务作为生成器，使用类似于“生成区间数”
### 生成模式(MergeType):
* 类型:MergeType 默认值:Append  
* 没有描述
### 子任务-选择(ETLSelector):
* 类型:可编辑选项 
* 输入或选择调用的子任务的名称
### 调用范围(ETLRange):
* 类型:String 
* 设定调用子任务的模块范围，例如2:30表示被调用任务的第2个到第30个子模块将会启用，其他模块忽略，2:-1表示从第2个到倒数第二个启用，符合python的slice语法，为空则默认全部调用
### 属性映射(MappingSet):
* 类型:String 
* 源属性:目标属性列 多个映射中间用空格分割，例如A:B C:D, 表示主任务中的A,B属性列会以C,D的名称传递到子任务中
***
