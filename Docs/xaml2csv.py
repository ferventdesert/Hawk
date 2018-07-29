import re
import  os
import sys
import io
import pandas
#sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf8')
import codecs
with codecs.open('Hawk/Lang/DefaultLanguage.xaml',encoding='utf-8') as f:
    with codecs.open('Hawk/Lang/DefaultLanguage.csv','w',encoding='utf-8') as f2:
        ls=f.readlines()
        f2.write('key,chs\n')
        for i,l in enumerate(ls):
            if l.find('x:Key=')>0:
                key=l.split('"')[1]
                value=ls[i+1].strip().replace(',','，')
                f2.write('%s,%s\n'%(key,value))
                
        