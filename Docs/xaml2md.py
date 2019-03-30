# -*- coding: utf-8 -*-  
# generate Hawk doc 
from lxml import etree
import fileinput
import re
import os
import sys
import codecs
BAD = [
    0, 1, 2, 3, 4, 5, 6, 7, 8,
    11, 12,
    14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
    # Two are perfectly valid characters but go wrong for different reasons.
    # 38 is '&' which gives: xmlParseEntityRef: no name.
    # 60 is '<' which gives: StartTag: invalid element namea different error.
]
pattern = re.compile('\{\{([^\{\}]{2,30})\}\}') 
BAD_BASESTRING_CHARS = [chr(b) for b in BAD]
BAD_UNICODE_CHARS = [unichr(b) for b in BAD]


def remove_bad_chars(value):
    # Remove bad control characters.
    if isinstance(value, unicode):
        for char in BAD_UNICODE_CHARS:
            value = value.replace(char, u'')
    elif isinstance(value, basestring):
        for char in BAD_BASESTRING_CHARS:
            value = value.replace(char, '')
    return value


def clean_xaml(f):
    for line in f:
        ls = []
        for pos in range(0, len(line)):
            if unichr(line[pos]) < 32:
                line[pos] = None
    ls.append(''.join([c for c in line if c]))
    return '\n'.join(ls)

def replace(dic,k):
    if k not in dic:
        print ("Key word %s not exists in file"%k)
        return None
    v= dic[k].strip()
    v= '\n'.join([r.strip() for r in v.split('\n')])
    items= pattern.findall(v)
  
    for item in items:
        if unicode(item)==k:
            print('Not Support invoke self! \n' +k)
            exit()
        repl= replace(dic,item)
        if repl is None:
            continue
        else:

            v= v.replace('{{'+item+'}}',repl) 
    dic[k]=v
    return v

def generate_dict(xaml_path):
    string = codecs.open(xaml_path,
                        encoding='utf-8').read()
    string2 = remove_bad_chars(string)

    result=etree.XML(string2)
    nodes = result.xpath('*')
    dic={}
    for node in nodes:
        key=node.attrib['{http://schemas.microsoft.com/winfx/2006/xaml}Key']
        preserve=  node.attrib.get('{http://www.w3.org/XML/1998/namespace}space') is None
        dic[key]= node.text
    for  file in os.listdir('resource'):
        if not file.endswith('.md'):
            continue
        name= file.split('.')[0]
        dic[name]= codecs.open('resource/'+file,encoding='utf-8').read()
    for k in dic.keys():
        replace(dic,k)
    return dic

def generate_doc(dic,target='hawk_doc',output_folder='hawk'):
    file=None
    pos=[0,0,0,0,0]
    titles=[]
    single_file= False
    for l in dic[target].split('\n'):
        if l.startswith('#'):
            splits= [r for r in l.split(' ') if r.strip()!='']
            header=splits[0]
            count= len(header)
            if count>6:
                print(l)
            pos[count-1]+=1
            for i in range(count,len(pos)):
                pos[i]=0
            if count==1:
                if not single_file and  file is not None:
                    file.close()
                title= 'index' if pos[0]==1 else splits[1]
                if single_file:
                    title='Hawk_Total_Doc'
                titles.append(title)
                if (not single_file) or pos[0]==1:
                    path=  '%s/docs/%s.md'%(output_folder,title)
                    #print(path)
                    file= codecs.open(path,'w',encoding='utf-8')
            elif count<4 and (pos[0]<9 or pos[0]>13):
                l= '%s %s.%s'%(header,'.'.join([str(r) for r in pos[1:count]]),splits[1])
        
        file.write(l+'\n')
    # with codecs.open('%s/mkdocs.yml'%output_folder, 'w',encoding='utf-8') as f:
    #     f.write('site_name: Hawk doc\n')
    #     f.write('nav:\n')
    #     for i,file in enumerate(titles):
    #         if i==0:
    #             title= 'Hawk' 
    #         else:
    #             title= file
    #         f.write("    - '%s': '%s.md'\n"%(title,file))
    #     f.write('theme: readthedocs\n')
    #     f.close()

def usage():
    help='''
    python xaml2md.py -i YOUR_XAML_RESOURCE -o OUTPUT_DIR
    YOUR_XAML_RESOURCE and OUTPUT_DIR can be ignored
    '''
    print(help)
if __name__ == '__main__':

    import sys, getopt
    opts, args = getopt.getopt(sys.argv[1:], "hi:o:")
    input_file='../Hawk/Lang/zh-CN.xaml'
    #input_file='en_us.xaml'
    output_folder="hawk"
    for op, value in opts:
        if op == "-i":
            input_file = value
        elif op == "-o":
            output_folder = value
        elif op == "-h":
            usage()
            sys.exit()
       

    print(input_file)
    dic=generate_dict(input_file)

    generate_doc(dic,output_folder=output_folder)



