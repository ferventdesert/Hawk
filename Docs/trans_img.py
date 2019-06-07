import requests
import os
img_dir= 'imgs/'
def query(lines,func):
    for l in lines:
        if func(l):
            return l
def lfind(string, char):
    pos=string.find(char)
    return string[pos+1:]
def download(url,name,dir):
    name= name.replace('{','').replace('}','')
    print(url)
    if url.find('github')>0:
        return url
    r=requests.get(url)
    back= url.split('.')[-1].lower()
    if back not in ['jpg','png','gif']:
        back='jpg'
    filename= os.path.join(dir,name+'.'+back)
    newname= os.path.join("https://raw.githubusercontent.com/ferventdesert/Hawk/master/Docs/imgs",  name+'.'+back)
    if os.path.exists(filename):
        return newname
    with open(filename,"wb") as f:
        f.write(r.content)
    return newname
def parse_repl(string):
    lines= string.split('\n')
    new_lines=[]
    for l2  in lines:
        l=l2.strip()
        print(l)
        if l.startswith('!'):
            if l.endswith(']'):
                num= l.split('[')[2].split(']')[0]
                name= l.split('[')[1].split(']')[0]
                if name=="":
                    print('*************')
                    print(l)
                old_name= '[%s]'%num
                ref= query(lines,lambda x:x.strip().startswith(old_name))
                if ref is None:
                    print('>>>>>>>>>>>>>>>>')
                    print(l)
                    continue
                real_ref= lfind(ref,':').strip()
                new_ref= download(real_ref,name,img_dir)     

                new_lines.append(l.replace(old_name,'('+new_ref+')'))
            elif l.endswith(')'):
                name= l.split('[')[1].split(']')[0]
                if name=="":
                    print('*************')
                    print(l)
                ref= l.split('(')[1].split(')')[0]
                new_ref= download(ref,name,img_dir) 
                new_lines.append(l.replace(ref,new_ref))
        else:
            new_lines.append(l2)
    return '\n'.join(new_lines)


if __name__ == "__main__":
    import codecs
    dir= 'resource'
    fname='../Hawk/Lang/en-US.xaml'
    print(fname)
    f=  codecs.open(fname,encoding='utf-8').read()
    newl = parse_repl(f)
    
    with codecs.open(fname, 'w',encoding='utf-8') as newf:
        newf.write(newl) 
    exit()
    for name in os.listdir(dir):
        if not name.lower().endswith('md'):
            continue
        fname= os.path.join(dir,name)
        print(fname)
        f=  codecs.open(fname,encoding='utf-8').read()
        newl = parse_repl(f)
        
        with codecs.open(fname, 'w',encoding='utf-8') as newf:
            newf.write(newl) 
                

                