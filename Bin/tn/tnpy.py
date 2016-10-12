# coding=utf-8
import re
import itertools;
import sys

PY2 = sys.version_info[0] == 2
int_max = 9999999;


def isstr(s):
    if PY2:
        if isinstance(s, (str, unicode)):
            return True
    else:
        if isinstance(s, (str)):
            return True;
    return False;


def findany(iteral, func):
    for r in iteral:
        if func(r):
            return r;
    return None;


def getindex(iteral, func):
    for r in range(len(iteral)):
        if func(iteral[r]):
            return r;
    return -1;


def __GetPublicRoute(m):
    from collections import deque
    d = deque()
    route = []
    route.append(m.Entity.order)
    d.append(m)
    while True:
        if len(d) == 0:
            break
        m = d.popleft()
        route.append(m.mindex)
        m = m.children
        while m is not None:
            d.append(m)
            m = m.NextMatch
    return route


class MatchResult(object):
    def __init__(self, entity, match, start, children=None, rewrite=None):
        super(MatchResult, self).__init__()
        self.order = 0
        self.mindex = 0
        self.PropertyName = ""
        self.children = children
        self.entity = entity
        self.match = match
        self.rewrite = rewrite;
        self.pos = start
        self.IsShouldRewrite = None;
        self.CanSplit = False;

    def GetShouldRewrite(self):
        if self.IsShouldRewrite != None:
            return self.IsShouldRewrite;
        if self.children is None:
            if self.entity is None:
                return False;
            if isinstance(self.entity, ScriptEntity) == False and self.entity.Rewrite is None:
                return False;
            else:
                return True;
        else:
            r = False;
            order = 0;
            ms = self.children;
            for m in ms:
                if order != m.order:  # order diff must be rewrite
                    r = True;
                    break;
                order += 1;
                r |= m.GetShouldRewrite();
            self.IsShouldRewrite = r;
            return r;

    def RewriteItem(self):
        if self.rewrite is not None:
            return self.rewrite;
        if not self.IsShouldRewrite:
            self.rewrite = self.match;

        if self.children is None:
            self.rewrite = self.entity.RewriteItem(self.match)
            return self.rewrite;

        match = self.children[:];
        match = sorted(match, key=lambda m: m.order);
        frewrite = "";
        for m in match:
            frewrite += m.RewriteItem();
        self.rewrite = frewrite;
        if isinstance(self.entity, ScriptEntity):
            self.rewrite = self.entity.RewriteItem(self.children);
        return self.rewrite

    def __str__(self):
        return self.match;

    def ExtractDocument(self, document, mode=0):
        childDoc = {};
        if isinstance(self.entity, RepeatEntity):
            childDoc0 = [];
            for m in self.children:
                m.ExtractDocument(childDoc0, 1)
            if len(self.PropertyName) != 0 and len(childDoc0) > 0:
                document['$' + self.PropertyName] = childDoc0;
        elif self.children is not None:
            for m in self.children:
                m.ExtractDocument(childDoc)
        if mode == 0:
            if len(self.PropertyName) != 0:
                if self.PropertyName == '$value':
                    document[document['$key']] = self.RewriteItem();
                    del document['$key']
                else:
                    if RegexCore.ExtractDictEnabled:
                        if len(childDoc) != 0:
                            document['$' + self.PropertyName] = childDoc

                    value = self.RewriteItem();
                    name = getPropertyName(document, self.PropertyName, value);
                    if name is not None:
                        document[name] = value;
            else:
                if len(childDoc) != 0:
                    for r in childDoc:
                        document[r] = childDoc[r]
        else:
            document.append(childDoc);


def getPropertyName(dic, name, value):
    if name not in dic:
        return name;
    if dic[name] == value:
        return name;
    c = {r: dic[r] for r in dic.keys() if r.split('_')[0] == name};
    if value in c.values():
        return None;
    return "%s_%d" % (name, len(c));


class EntityBase(object):
    def __init__(self):
        self.Script = None
        self.order = 0
        self.Name = ""
        self.Rule = ""
        self.Type = ""
        self.Core = None
        self.Start = False;
        self.Rewrite = None;

    def RewriteItem(self, input):
        m = self.MatchItem(input, 0, None, True);
        return m.RewriteItem();

    def RebuildEntity(self):
        pass;

    def SetValues(self, values):
        if isinstance(values, dict):
            value = values.get("Order", None);
            if value is not None:
                self.order = int(value);
            value = values.get("Type", None);
            if value is not None:
                self.Type = value;
            value = values.get("Parameter", None);
            if value is None:
                return;
            if value.find('|') >= 0:
                return;
            va = value.split(',');
            for v in va:
                vs = v.split('=');
                if len(vs) != 2:
                    continue;
                key, value = vs[0].strip(), vs[1].strip();
                value = eval(value);
                setattr(self, vs[0].strip(), value);

    def EvalScript(self, m, input=None):
        if self.Script == '':
            return True
        if not isstr(self.Script):
            return self.Script(self, m)
        if input is None:
            input = m[0].match;
        core = self.Core

        def check(condition, result, elsework=None):
            if eval(condition):
                r = eval(result);
                return r;
            elif elsework is not None:
                r = eval(elsework);
                return r;

        def invoke(func, para):
            return eval(func)(para);

        def e(entityname):
            entity = self.Core.Entities[entityname]
            header = None
            header = entity.MatchItem(input, 0, True, header)
            if not IsFail(header):
                header = MatchResult(entity, None, -100)
                return header
            return None

        def dist(name, i=0):
            header = e(name)
            if header is None:
                return int_max;
            return abs(header.pos - m[i].pos)

        result = eval(self.Script)
        return result

    def LogIn(self, input, start, end=None):
        if self.Core is None or self.Core.LogFile is None:
            return

        if self.Core.LogFile.name.find('htm') < 0:
            if end is not None:
                end = start + 200;
            input = input[start: end].replace('\n', '<\\n>').replace('\r', '<\\r>');

            self.Core.LogFile.write(' ' * self.Core.matchLevel * 2)
            if self.Core.LogFile.encoding != 'utf-8':
                input = input.encode('utf-8')
            self.Core.LogFile.write('%s,Raw  =%s\n' % (str(self), input))
        else:
            if self.Core.LogFile.encoding != 'utf-8':
                input = input.encode('utf-8')
            self.Core.LogFile.write('<p>' + '&nbsp;' * self.Core.matchLevel * 4)
            self.Core.LogFile.write('%s,Raw= <font color="#FF0000">%s</font></p>\r' % (str(self), input))
        self.Core.matchLevel += 1

    def LogOut(self, match, buffered=False):
        if self.Core is None or self.Core.LogFile is None:
            return
        if self.Core.LogFile.encoding != 'utf-8' and match is not None:
            match = match.encode('utf-8')
        self.Core.matchLevel -= 1
        if self.Core.LogFile.name.find('htm') < 0:
            self.Core.LogFile.write(' ' * self.Core.matchLevel * 2)
            if match is not None:
                match = match[:200].replace('\n', '<\\n>').replace('\r', '<\\r>');

                self.Core.LogFile.write('%s,%s=%s\n' % (str(self), ('Buff ' if buffered else 'Match'), match))
            else:
                self.Core.LogFile.write('%s,NG\n' % str(self))
        else:

            self.Core.LogFile.write('<p>' + '&nbsp;' * self.Core.matchLevel * 4)
            if match != None:
                self.Core.LogFile.write('%s,<b>OK</b>,Raw= <font color="#FF0000">%s</font></p>\r' % (str(self), match))
            else:
                self.Core.LogFile.write('%s,<font color="#FF0000"><b>NG</b></font></p>\r' % str(self))

    def MatchItem(self, input, start, end, muststart, mode=None):
        return None;

    def GetName(self):
        name = self.Name if self.Name != "" else "unknown"
        return "%s,%s" % (
            name, findany(re.split("[,.']", str(type(self))), lambda d: d.find('Entity') > 0).replace("Entity", ""))

    def __str__(self):
        return self.GetName()


class StringEntity(EntityBase):
    def __init__(self, match="", rewrite=None, condition=''):
        super(StringEntity, self).__init__()
        self.Match = match
        self.Rewrite = rewrite
        self.Condition = condition

    def RewriteItem(self, input):
        if None == self.Rewrite:
            return input
        return input.replace(self.Match, self.Rewrite);

    def SetValues(self, values):
        super(StringEntity, self).SetValues(values);
        if isinstance(values, dict):
            return;
        self.Match = values[0]
        if len(values) > 1:
            self.Rewrite = values[1]

    def MatchItem(self, input, start, end, muststart, mode=None):
        self.LogIn(input, start)
        if end is None:
            end = int_max;
        pos = input.find(self.Match, start, end)
        if pos < 0 or (muststart == True and pos != start):
            self.LogOut(None, False)
            return int_max if pos < 0 else pos;

        self.LogOut(self.Match)
        m = MatchResult(self, self.Match, pos)
        m.rewrite = self.Match if self.Rewrite is None else self.Rewrite;
        return m;


class RefEntity(EntityBase):
    def __init__(self, name):
        self.RuleName = name;

    def RebuildEntity(self):
        self.Entity = self.Core.Entities.AllEntities[self.RuleName];

    def MatchItem(self, input, start, end, muststart, mode=None):
        return self.Entity.MatchItem(input, start, end, muststart, mode);

    def RewriteItem(self, input):
        return self.Entity.RewriteItem(input);


class RepeatEntity(EntityBase):
    def __init__(self, entity=None, least=1, most=1, equal=False):
        super(RepeatEntity, self).__init__()
        self.Least = least
        self.Most = most
        self.Entity = entity
        self.Equal = equal;

    __splitre = re.compile('[,{}]');

    def RebuildEntity(self):
        if isstr(self.Entity):
            self.Entity = self.Core.Entities[self.Entity];
        self.Entity.Core = self.Core;

    def SetValues(self, values):
        super(RepeatEntity, self).SetValues(values);
        if isinstance(values, dict):
            return
        cal = values[0];
        if cal == '*':
            self.Least = 0
            self.Most = -1
        elif cal == '+':
            self.Least = 1
            self.Most = -1
        elif cal == '?':
            self.Least = 0
            self.Most = 1
        elif cal.startswith('{'):
            sp = self.__splitre.split(cal);
            self.Least = int(sp[1])
            self.Most = int(sp[2])
        if self.Most == -1:
            self.Most = 99999;

    def MatchItem(self, input, start, muststart, mode=None):
        self.LogIn(input, start)
        right = 0
        start = start
        lresult = None
        isStop = False;
        isReset = False;
        bestResults = [];

        omax = -1;
        while right < self.Most:
            result = self.Entity.MatchItem(input, start, muststart, None)
            if not IsFail(result):
                if right == 0:
                    start = result.pos
                    bestResults.append(result);
                else:
                    if self.Equal:
                        if result.pos != start or lresult.match != result.match:
                            if not isinstance(self.Entity, RepeatEntity):
                                isStop = True;
                            else:
                                if omax == -1:
                                    omax = self.Entity.Most;
                                    self.Entity.Most = self.Entity.Least;
                                else:
                                    self.Entity.Most += 1;
                                if self.Entity.Most >= omax:
                                    isStop = True;
                                right = 0;
                                start = 0;
                                isReset = True;
                        else:
                            bestResults.append(result)

                    elif result.pos != start:
                        isStop = True;
                    else:
                        bestResults.append(result)
                if isStop:
                    break
                if not isReset:
                    lresult = result;
                    start = result.pos + len(result.match);
                    lresult.order = right;
                    right += 1
                isReset = False;

            else:
                break;
        if right < self.Least:
            self.LogOut(None, False)
            return start;
        pos = start
        matchResultString = input[start:start]
        if bestResults == []:  # this is ? or * ,can be null
            bestResult = MatchResult(None, '', 0);
            bestResult.rewrite = '';
        p = MatchResult(self, matchResultString, pos, bestResults)
        self.LogOut(matchResultString)
        return p;


class DiffEntity(EntityBase):
    def __init__(self, universe=None, complements=None):
        super(DiffEntity, self).__init__()
        self.Universe = universe
        self.Complements = complements if complements is not None else [];

    def RebuildEntity(self):
        if isstr(self.Universe):
            self.Universe = self.Core.Entities[self.Universe];
        for r in range(len(self.Complements)):
            if isstr(self.Complements[r]):
                self.Complements[r] = self.Core.Entities[self.Complements[r]];

    def MatchItem(self, input, start, end, muststart, mode=None):
        self.LogIn(input, start)
        unresult = self.Universe.MatchItem(input, start, end, muststart, None)
        if IsFail(unresult):
            self.LogOut(None)
            return unresult;
        matchResult = None
        if len(self.Complements) != 0:
            for en in self.Complements:
                matchResult = en.MatchItem(unresult.match, 0, None, True, matchResult)
                if IsFail(matchResult):
                    self.LogOut(None)
                    return unresult.pos;
        p = MatchResult(self, unresult.match, unresult.pos, [unresult])
        self.LogOut(unresult.match)
        return p;


class RegexEntity(StringEntity):
    def __init__(self, match="", rewrite=None):
        super(RegexEntity, self).__init__(match,rewrite)
        self.regex = None
        self.merge = False;
        self.IsMatchMax = True;
        if self.Match != "":
            self.RebuildEntity();

    def RewriteItem(self, input):
        if self.Rewrite is None:
            return input
        m = self.regex.search(input);
        return self.__Replace(m, self.Rewrite)

    def RebuildEntity(self):
        if self.regex is None:
            try:
                self.regex = re.compile(self.Match)
            except:
                print("Regex Format error %s" % (self.Match));

    def SetValues(self, values):
        super(RegexEntity, self).SetValues(values);
        if isinstance(values, dict):
            return;
        self.Match = values[0]
        if len(values) > 1:
            if isstr(values[1]):
                self.Rewrite = values[1]
            else:
                self.merge = True;
                self.maps = values[1];

        try:
            self.regex = re.compile(self.Match)
        except:
            print("Regex Format error %s" % (self.Match));

    def __Replace(self, m, string):
        if (m.lastindex != None):
            c = m.lastindex + 1;
        else:
            c = 1;
        for index in range(c):
            string = string.replace('$' + str(index), m.group(index)).replace('\\n', '\n');
        return string;

    def MatchItem(self, input, start, end, muststart, mode=None):
        self.LogIn(input, start)
        if end is None:
            if muststart:
                m = self.regex.match(input, start);
            else:
                m = self.regex.search(input, start);
        else:
            if muststart:
                m = self.regex.match(input, start, end);
            else:
                m = self.regex.search(input, start, end)
        if m is None or (muststart == True and m.start() != start):
            self.LogOut(None)
            return int_max if m is None else m.start();

        p = MatchResult(self, m.group(), m.start())
        if self.merge:

            p.rewrite = self.maps[p.match].RewriteItem(p.match);
        elif self.Rewrite is None:
            p.rewrite = p.match;
        else:
            p.rewrite = self.__Replace(m, self.Rewrite);
        self.LogOut(m.group())
        return p;


class ScriptEntity(EntityBase):
    def __init__(self, script=""):
        super(ScriptEntity, self).__init__()
        self.Script = script

    def SetValues(self, values):
        super(ScriptEntity, self).SetValues(values);
        if isinstance(values, list):
            self.Script = values[0]

    def RewriteItem(self, match):
        res=self.EvalScript(match);
        return res;

    def MatchItem(self, input, start, end, muststart, mode=None):
        core = self.Core;
        return eval(self.Script);

    def MatchItem2(self, origin, rewritetarget, isrewrite=False):
        input = rewritetarget.match;
        self.LogIn(input, rewritetarget.pos)
        if isrewrite:
            r = input;
        else:
            r = self.EvalScript(None, origin, input);
            if r is None:
                return None;
            pos = input.find(r);
            if pos < 0:
                return None;
        p = MatchResult(self, r, rewritetarget.pos, [rewritetarget])
        self.LogOut(r)
        return p;


def IsFail(x):
    if isinstance(x, int):
        return True;
    return False;


def BinarySearchIndex(arr, v):
    l = len(arr);
    if l == 0:
        return 0;
    left = 0;
    right = l - 1;
    while left <= right:
        mid = (left + right) >> 1;
        if arr[mid] > v:
            right = mid - 1;
        elif arr[mid] < v:
            left = mid + 1;
        else:
            break;
    if v < arr[mid]:
        return mid;
    elif v == arr[mid]:
        if mid % 2 == 0:
            return mid + 1;
        else:
            return mid;
    return mid + 1;


def IsInExtractArea(area, pos):
    i = BinarySearchIndex(area, pos);
    if i % 2 == 0:
        return pos;
    return area[i];


class TableEntity(EntityBase):
    def __init__(self, tables=None, groups=None):
        super(TableEntity, self).__init__()
        self.Tables = tables if tables is not None else [];
        self.Properties = {}
        self.Group = groups if groups is not None else [];
        self.IsMatchMax = True;

    def ReplaceEscapeChar(self, s):
        for c in list('+-*().'):
            s = s.replace(str(c), '\\' + str(c));
        return s;

    def RebuildEntity(self):
        if not isinstance(self.Tables, list):
            raise Exception('TableEntity must be array')
        for r in range(len(self.Tables)):
            if isstr(self.Tables[r]):
                self.Tables[r] = self.Core.Entities[self.Tables[r]];
            if isinstance(self.Tables[r], SequenceEntity):
                self.Tables[r].RebuildEntity();
            self.Tables[r].Core = self.Core;

        if not RegexCore.AutoMerge:
            return;

        seqs = [m for m in self.Tables if isinstance(m, StringEntity) and not isinstance(m,RegexEntity)];
        if len(seqs) < 2:
            return;
        ms = {};
        rex = RegexEntity();
        rex.Name = self.Name + "_merge";
        rex.Core = self.Core;
        match = "";

        for r in seqs:
            m = self.ReplaceEscapeChar(r.Match);
            ms[r.Match] = r;
            match += m + '|';
        match = match[:-1];

        rex.SetValues([match, ms]);
        for r in seqs:
            self.Tables.remove(r);
        self.Tables.append(rex);
        for t in self.Tables:
            t.Core = self.Core;
        return

    def SetValues(self, values):
        super(TableEntity, self).SetValues(values);
        if isinstance(values, list):
            return;
        value = values.get("Property", None);
        if value is not None:
            items = [x.strip() for x in value.split('|')]
            for i in range(min(len(items), len(self.Tables))):
                str2 = [x.strip() for x in items[i].split(',')]
                self.Properties[i] = str2

    def MatchItem(self, input, start, end, muststart, mode=None):
        self.LogIn(input, start)
        bestLen = -1
        bestSeqID = -1
        bestStart = int_max;
        rpos = bestStart;
        submode = None;
        dictbuf = self.Core.Entities.SeqBuff
        bestMatchResult = None
        total = len(self.Tables)
        for seqid in range(total):
            if seqid in self.Group and bestSeqID != -1 and bestStart == start:
                break;
            entity = self.Tables[seqid]
            if mode is not None and mode.mindex != -1:
                if seqid != mode.mindex:
                    continue
                submode = mode.children;
            seqValue = dictbuf.GetMatch(entity, input, start, end);
            if seqValue == -1:
                continue
            if seqValue is not None:
                theader = seqValue;
            else:
                theader = entity.MatchItem(input, start, end, muststart, submode)
                if IsFail(theader):
                    rpos = min(theader, rpos);
                    if not muststart:
                        dictbuf.AddScan(entity, start, start);
                    if submode is not None and mode.mindex == -1:
                        mode.mindex = -1;
                        submode = None;
                else:
                    dictbuf.AddScan(entity, start, theader.pos);
                    dictbuf.AddEntity(entity, theader)
            if IsFail(theader):
                continue
            spos, slen = theader.pos, len(theader.match);
            if (spos < bestStart or (
                            spos == bestStart and (slen > bestLen if self.IsMatchMax == True else slen < bestLen))):
                bestLen = slen
                bestStart = spos
                bestSeqID = seqid
                bestMatchResult = theader;

        if bestMatchResult is not None:
            if bestMatchResult.children is not None:
                match = bestMatchResult.children
            else:
                match = [bestMatchResult];

            if len(self.Properties) > bestSeqID:
                index = 0;
                for element in self.Properties[bestSeqID]:
                    match[index].PropertyName = element
                    index += 1;
                    if index >= len(match):
                        break
            bestMatchResult = MatchResult(self, bestMatchResult.match, bestMatchResult.pos, [bestMatchResult]);
            bestMatchResult.mindex = bestSeqID;
            self.LogOut(bestMatchResult.match)
            return bestMatchResult;
        self.LogOut(None)
        return rpos;


def AddArea(sb, start, end):
    l = len(sb);
    insp = 0;
    if l == 0:
        sb.append(start);
        sb.append(end);
    else:
        left = 0;
        right = l - 2;
        while left <= right:
            mid = ((left + right) >> 2) << 1;
            if sb[mid] < start:
                left = mid + 2
                if right < left:
                    if l <= left:
                        sb.append(start)
                        sb.append(end)
                        insp = l
                    else:
                        sb.insert(left, start)
                        sb.insert(left + 1, end)
                        insp = left;
                        break
            elif sb[mid] > start:
                right = mid - 2
                if right < left:
                    sb.insert(left, start)
                    sb.insert(left + 1, end)
                    insp = left;
                    break
            else:
                if sb[mid + 1] < end:
                    sb[mid + 1] = end
                break

    l = len(sb);
    if insp > 2:
        i = insp - 2;
    else:
        i = 0;
    while i < l:
        pi = i - 2
        if pi < 0 or sb[pi + 1] < sb[i] - 1:
            pi += 2;

        else:
            if sb[pi + 1] <= sb[i + 1]:
                sb[pi + 1] = sb[i + 1];
            del sb[pi + 2:pi + 4];
            l -= 2;
            i -= 2;
        i += 2;
    return sb;


class BuffHelper(object):
    def __init__(self, slen):
        self.scanbuf = {};
        self.entitybuf = {};
        self.slen = slen;
        self.extractedarea = [];

    def AddEntity(self, entity, matchResult):
        entityid = id(entity);
        sb = self.entitybuf.get(entityid, None);
        if sb is None:
            sb = [];
            self.entitybuf[entityid] = sb;
        lo = 0
        hi = len(sb)
        while lo < hi:
            mid = (lo + hi) // 2
            if matchResult.pos < sb[mid].pos:
                hi = mid
            else:
                lo = mid + 1
        sb.insert(lo, matchResult)

    def AddScan(self, entity, start, end=None):

        if entity != 0:
            entityid = id(entity);
            sb = self.scanbuf.get(entityid, None);
        else:
            sb = self.extractedarea;
        if sb is None:
            sb = [];
            self.scanbuf[entityid] = sb;
        if end is None:
            end = self.slen;
        AddArea(sb, start, end);

    def GetMatch(self, entity, input, start, end):

        entityid = id(entity);
        sb = self.scanbuf.get(entityid, None);
        if sb is None:
            return None;
        i = BinarySearchIndex(sb, start);
        if i % 2 == 0:
            return None;
        start = start;
        end = sb[i]
        eb = self.entitybuf.get(entityid, None);
        if eb is None:
            entity.LogIn(input, start, end)
            entity.LogOut(None, True);
            return end;
        hi = len(eb)
        lo = 0;
        while lo < hi:
            mid = (lo + hi) // 2
            if start < eb[mid].pos:
                hi = mid
            elif start == eb[mid].pos:
                lo = mid;
                break;
            else:
                lo = mid + 1
        if lo >= len(eb):
            entity.LogIn(input, start, end)
            entity.LogOut(None);
            return None;
        if eb[lo].pos <= end:
            entity.LogIn(input, start, end)
            entity.LogOut(eb[lo].match, True)
            return eb[lo];
        return None;


class TreeNode(object):
    def __init__(self):
        self.Left = None;
        self.Right = None;
        self.Root = None;
        self.Match = None;
        self.Rewrite = None;
        self.Index = 0;
        self.order = 0

    def GetLeft(self):
        tree = self;
        while tree.Left is not None:
            tree = tree.Left;
        return tree;

    def GetRight(self):
        tree = self;
        while tree.Right is not None:
            tree = tree.Right;
        return tree;

    def InorderTravel(self, node, func):
        if node is None:
            return;
        self.InorderTravel(node.Left, func);
        func(node);
        self.InorderTravel(node.Right, func)


def IsSameValue(arr, l, r):
    if r < l + 2:
        return False;
    for i in range(l + 1, r):
        if arr[i] != arr[l]:
            return False;
    return True;


def GetMaxIndex(arr, l, r):
    max_value = -100;
    max_index = -1;
    for i in range(l, r):
        if arr[i] > max_value:
            max_index = i;
            max_value = arr[i];
    return max_index;


class SequenceEntity(EntityBase):
    def __init__(self, matchEntities=None, rewriteEntities=None, matchorders=None, rewriteorders=None, condition=None):
        super(SequenceEntity, self).__init__()
        self.DirectReplace = "directreplace"
        self.MatchEntities = matchEntities if matchEntities is not None else [];
        self.RewriteEntities = rewriteEntities if rewriteEntities is not None else [];
        self.Rewriteorders = rewriteorders if rewriteorders is not None else []
        self.Matchorders = matchorders if matchorders is not None else None;
        self.Properties = []
        self.Condition = condition;
        self.Root = None;

    def SetValues(self, values):
        super(SequenceEntity, self).SetValues(values);
        if isinstance(values, list):
            return;
        value = values.get("Property", None);
        if value is not None:
            self.Properties = [x.strip() for x in value.split(',')]

    def BuildMatchTree(self, l, r):
        if l > r or l >= len(self.Matchorders):
            return None;
        if r == l:
            tree = TreeNode();
            tree.Match = self.MatchEntities[l];
            return tree;
        if IsSameValue(self.Matchorders, l, r):
            tb = TableEntity();
            tb.Core = self.Core;
            for item in itertools.combinations(self.MatchEntities[l:r], r - l + 1):
                se = SequenceEntity(item);
                se.Core = tb.core;
                tb.Tables.append(se);
            tree = TreeNode();
            tree.Match = tb;
            return tree;
        max_index = GetMaxIndex(self.Matchorders, l, r)
        tree = TreeNode();
        tree.order = self.Matchorders[max_index];
        tree.Index = max_index;
        tree.Match = self.MatchEntities[max_index];
        if max_index < len(self.RewriteEntities):
            tree.Rewrite = self.RewriteEntities[self.Rewriteorders[max_index]]
        tree.Left = self.BuildMatchTree(l, max_index - 1);
        if tree.Left is not None:
            tree.Left.Root = tree;
        tree.Right = self.BuildMatchTree(max_index + 1, r);
        if tree.Right is not None:
            tree.Right.Root = tree;
        return tree;

    def RebuildEntity(self):
        if len(self.Rewriteorders) == 0:
            self.Rewriteorders = [i for i in range(0, len(self.MatchEntities))];
        for r in range(len(self.MatchEntities)):
            m = self.MatchEntities[r];
            if isstr(m):
                if m not in self.Core.Entities.EntityNames:
                    raise 'can not find rule %s' % (m)
                self.MatchEntities[r] = self.Core.Entities.EntityNames[m]
            self.MatchEntities[r].Core = self.Core;
        for r in range(len(self.RewriteEntities)):
            if isstr(self.RewriteEntities[r]):
                self.RewriteEntities[r] = self.Core.Entities[self.RewriteEntities[r]];
            self.RewriteEntities[r].Core = self.Core;
        if self.Matchorders is None:
            self.Matchorders = [i for i in range(len(self.MatchEntities), 0, -1)];

        self.Tree = self.BuildMatchTree(0, len(self.Matchorders));

    def TreeNodeMatch(self, treenode, input, start, end, finalmatchScript, muststart=False):
        dictbuf = self.Core.Entities.SeqBuff
        matchEntity = treenode.Match;
        matchResult = dictbuf.GetMatch(matchEntity, input, start, end);
        fail = False;
        if matchResult is None:
            matchResult = matchEntity.MatchItem(input, start, end, treenode.Left is None and muststart)
            if not IsFail(matchResult):
                dictbuf.AddScan(matchEntity, start, matchResult.pos);
                dictbuf.AddEntity(matchEntity, matchResult);
        if not IsFail(matchResult):
            if treenode.Right is None and end is not None and matchResult.pos + len(matchResult.match) != end:
                fail = True;
            rewriteEntity = None;
            if not finalmatchScript:
                rewriteEntity = treenode.Rewrite;
            if rewriteEntity is not None and rewriteEntity.Name != self.DirectReplace:
                if isinstance(rewriteEntity, ScriptEntity):
                    matchResult = rewriteEntity.MatchItem2(input, matchResult, True)
                    if matchResult is None:
                        fail = True;
                else:
                    matchResult.rewrite = rewriteEntity.RewriteItem(matchResult.match)
        if not fail and not IsFail(matchResult):
            mleft = matchResult.pos;
            mlen = len(matchResult.match);
            mright = mleft + mlen;
            runtree = TreeNode();
            runtree.Match = matchResult;
            if treenode.Left is not None:
                left = self.TreeNodeMatch(treenode.Left, input, start, mleft, finalmatchScript);
                if IsFail(left):
                    fail = True;
                elif muststart == True and left.GetLeft().Match.pos != start:
                    fail = True;
                else:
                    rm = left.GetRight().Match;
                    if rm.pos + len(rm.match) != mleft:
                        fail = True;
                runtree.Left = left;
            if not fail and treenode.Right is not None:
                right = self.TreeNodeMatch(treenode.Right, input, mright, end,
                                           finalmatchScript);

                if IsFail(right):
                    fail = True;
                else:
                    if mlen == 0:
                        matchResult.pos = right.GetLeft().Match.pos;
                    elif right.GetLeft().Match.pos != mright:
                        fail = True;
                    elif end is not None:
                        rright = right.GetRight().Match;
                        rpos = rright.pos + len(rright.match);
                        if rpos != end:
                            fail = True;

                runtree.Right = right;
        if IsFail(matchResult):
            return matchResult;
        elif fail:
            return matchResult.pos + len(matchResult.match);
        return runtree;

    def MatchItem(self, input, start, end, muststart, mode=None):
        self.LogIn(input, start)
        finalmatchScript = False;
        if len(self.MatchEntities) > 1 and len(self.RewriteEntities) == 1 and isinstance(self.RewriteEntities[0],
                                                                                         ScriptEntity):
            finalmatchScript = True;
        treeResult = self.TreeNodeMatch(self.Tree, input, start, end, finalmatchScript, muststart);
        if IsFail(treeResult):
            self.LogOut(None)
            return treeResult;
        matchResults = [];
        treeResult.InorderTravel(treeResult, lambda m: matchResults.append(m.Match));
        for i in range(len(matchResults)):
            if i < len(self.Properties):
                matchResults[i].PropertyName = self.Properties[i]
            if i < len(self.Rewriteorders):
                matchResults[i].order = self.Rewriteorders[i]
            else:
                matchResults[i].order = i
        if self.Condition is not None and self.Condition.EvalScript(matchResults, input) == False:
            self.LogOut(None)
            return start;
        start = matchResults[0].pos;
        sum = 0;
        for i in range(0, len(matchResults)):
            sum += len(matchResults[i].match);
        matching = input[start:start + sum];
        if finalmatchScript:
            script = self.RewriteEntities[0];
            p = MatchResult(script, matching, start, matchResults);
            return p;
        if len(matchResults) > 1:
            p = MatchResult(self, matching, start, matchResults)
        else:
            p = matchResults[0];
        self.LogOut(matching, False)
        return p;


class NoSequenceEntity(EntityBase):
    def __init__(self, matchEntities=None, rewriteEntities=None, minMatchs=None, maxMatchs=None, condition=None,
                 properties=None):
        super(NoSequenceEntity, self).__init__()
        self.DirectReplace = "directreplace"
        self.MatchEntities = matchEntities if matchEntities is not None else [];
        self.RewriteEntities = rewriteEntities if rewriteEntities is not None else [];
        self.MinMatchs = minMatchs if minMatchs is not None else [1 for i in range(len(matchEntities))];
        self.MaxMatchs = maxMatchs if maxMatchs is not None else [9999 for i in range(len(matchEntities))];
        self.Properties = properties if properties is not None else [];
        self.Condition = condition;

    def RebuildEntity(self):
        for r in range(len(self.MatchEntities)):
            if isstr(self.MatchEntities[r]):
                self.MatchEntities[r] = self.Core.Entities[self.MatchEntities[r]];
            self.MatchEntities[r].Core = self.Core;
        for r in range(len(self.RewriteEntities)):
            if isstr(self.RewriteEntities[r]):
                self.RewriteEntities[r] = self.Core.Entities[self.RewriteEntities[r]];
            self.RewriteEntities[r].Core = self.Core;

    def MatchItem(self, input, start, end, muststart, mode=None):
        self.LogIn(input, start)
        end = len(input) if end is None else end;
        finalmatchScript = False;
        dictbuf = self.Core.Entities.SeqBuff
        if len(self.MatchEntities) > 1 and len(self.RewriteEntities) == 1 and isinstance(self.RewriteEntities[0],
                                                                                         ScriptEntity):
            finalmatchScript = True;
        matchResults = [];
        for mi in range(0, len(self.MatchEntities)):
            fail = False;
            matchCount = 0;
            if fail:
                break;
            matchEntity = self.MatchEntities[mi];
            start0 = start;
            end0 = end;
            while start0 < end:
                matchResult = dictbuf.GetMatch(matchEntity, input, start0, None);
                if matchResult is None:
                    matchResult = matchEntity.MatchItem(input, start0, None, False)
                if IsFail(matchResult):
                    start0 = matchResult if matchResult != start0 else matchResult + 4;
                    continue;
                start0 = matchResult.pos + len(matchResult.match)
                dictbuf.AddScan(matchEntity, start0, matchResult.pos);
                dictbuf.AddEntity(matchEntity, matchResult);
                if matchResult.pos >= end0:
                    continue;
                rewriteEntity = None;
                if not finalmatchScript and mi < len(self.RewriteEntities):
                    rewriteEntity = self.RewriteEntities[mi];
                if rewriteEntity is not None and rewriteEntity.Name != self.DirectReplace:
                    if isinstance(rewriteEntity, ScriptEntity):
                        matchResult = rewriteEntity.MatchItem2(input, matchResult, True)
                        if matchResult is None:
                            start0 += 1;
                            continue;
                        else:
                            matchResult.rewrite = rewriteEntity.RewriteItem(matchResult.match)
                matchResults.append(matchResult);
                matchCount += 1;
                if mi < len(self.Properties):
                    matchResult.PropertyName = self.Properties[mi]
                if matchCount >= self.MaxMatchs[mi]:
                    break;
            if matchCount < self.MinMatchs[mi]:
                fail = True;
                break;
        if fail == True:
            return end if end is not None else len(input);
        if self.Condition is not None and self.Condition.EvalScript(matchResults, input) == False:
            self.LogOut(None)
            return start;

        matchResults = sorted(matchResults, key=lambda m: m.pos);
        start = matchResults[0].pos;
        end = matchResults[-1].pos + len(matchResults[-1].match);
        matching = input[start:end];
        if finalmatchScript:
            script = self.RewriteEntities[0];
            p = MatchResult(script, matching, start, matchResults);
            return p;
        if len(matchResults) > 1:
            p = MatchResult(self, matching, start, matchResults)
        else:
            p = matchResults[0];
        pos = start;
        rewrite = ""
        for m in matchResults:
            m.GetShouldRewrite();
            m.RewriteItem()
            rewrite += input[pos:m.pos];
            rewrite += m.rewrite;
            pos = m.pos + len(m.match);
        rewrite += input[pos:end];
        p.rewrite = rewrite;
        self.LogOut(matching, False)
        return p;


class Entities(object):
    def __init__(self):
        super(Entities, self).__init__()
        self.AllEntities = []
        self.ValidEntities = []
        self.EntityNames = {}
        self.EntityIds = {}

    def appendids(self, entity):
        if -1 != entity.order:
            self.EntityIds[entity.order] = entity
        self.ValidEntities.append(entity)

    def append(self, entity):
        if entity.Name is not None:
            self.EntityNames[entity.Name] = entity
        self.AllEntities.append(entity)

    def __getitem__(self, item):
        if item in self.EntityNames:
            return self.EntityNames[item]
        else:
            print("Entity name %s can not be found!" % (item));
        for entity in self.AllEntities:
            if (entity.Name == item):
                return entity
        return None


class Token:
    (NAME, ENTITY, MINUS, COLON, END, BAR, REPEAT, Script) = range(8)


class RegexToken:
    def __init__(self, regex, token, count, type=None):
        self.Regex = regex
        self.Token = token
        self.Count = count
        self.EntityType = type


class TokenItem:
    def __init__(self, token):
        self.Rule = None
        self.Token = token
        self.Entity = None
        self.Values = []


class RegexCore(object):
    AutoModeStudy = True
    ExtractDictEnabled = False
    LogFile = None
    matchLevel = 0
    AutoMerge = True
    MatchAllEntity = False;

    def __init__(self, rule=None):
        super(RegexCore, self).__init__()
        self.Entities = None
        self.__entity_name = re.compile(r"^(\w+)\s*=\s*")
        self.__entity_reexp = re.compile(r"^\(/((?:.(?!/\)))*?)/\s*:\s*/((?:(?!\(/).)*?)/\)\s*")
        self.__entity_string = re.compile("^\(\"((?:(?!\"\)).)*?)\"\s*:\s*\"(.*?)\"(?:\s*:\s*\"(.*?)\")?\s*\)\s*")
        self.__numbeRegex = re.compile(r"[0-9]+")
        self.__r_bar = re.compile(r"^\s*\|\s*")
        self.__r_bar2 = re.compile(r"^\s*/\s*")
        self.__r_colon = re.compile(r"^\s*:\s*")
        self.__r_conds = re.compile(r'^\s*\"([^"]*)\"')
        self.__r_entity = re.compile("^\$\((\w+)\)\s*")
        self.__r_minus = re.compile(r"^\s*-\s*")
        self.__r_order = re.compile(r"^\$0*(\d+)\s*")
        self.__r_reexp = re.compile(r"^\(/((?:.(?!/\s*:\s*/))*?)/\)\s*")
        self.__r_repeat = re.compile(r"^\s*([*?+]|{(\d+),(-1|\d+)})\s*")
        self.__r_semicolon = re.compile(r"^\s*;")
        self.__r_string = re.compile(r"^\(\s?\"(.*?)\"\s?\)\s*")
        self.tnFileName = None
        self.Entities = Entities()
        self.Entities.AllEntities = []
        self.Entities.ValidEntities = []
        if rule is not None:
            self.InitTNRule(rule);

    def RebuildEntity(self):
        for entity in self.Entities.AllEntities:
            entity.RebuildEntity();
        self.Entities.ValidEntities = sorted(self.Entities.ValidEntities, key=lambda x: x.order)

    def InitPyRule(self, pyrule):
        for r in pyrule.__dict__:
            s = getattr(pyrule, r);
            if isinstance(s, EntityBase):
                s.Core = self;
                s.Name = r;
                self.Entities.append(s);
                if s.order != 0:
                    self.Entities.appendids(s);

    def WriteHTMLHeader(file):
        file.write(
            '''<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"><html xmlns="http://www.w3.org/1999/xhtml"><head><meta http-equiv="Content-Type" content="text/html; charset=utf-8" /><title>{title}}</title></head><body>''');

    def WriteHTMLEnd(file):
        file.write('''</body></html>''');

    def ToHTML(self, newfile):
        file_object = open(self.tnFileName, 'r', 'utf-8')
        newfile = open(newfile, 'w', 'utf-8')
        text = file_object.read()
        texts = text.split('\n')
        r_entity = re.compile(r"\$\((\w+)\)\s*")
        self.WriteHTMLHeader(newfile)
        for t in texts:
            m = self.__entity_name.match(t)
            if m != None:
                mt = m.group(1)
                t = t.replace(mt, '<a name="%s"><b>%s</b></a>' % (mt, mt), 1)
            m = r_entity.findall(t)
            if len(m) > 0:
                for mt in m:
                    t = t.replace(mt, '<a href="#%s">%s</a>' % (mt, mt))
            if t.startswith("#"):
                newfile.write('<p><font color="#909090">%s</font></p>\n' % t)
            else:
                newfile.write('<p>%s</p>\n' % t)

        self.WriteHTMLEnd(newfile)
        newfile.close()
        file_object.close()

    def InitRuleText(self, text, addtoorder=True):
        propertyregex = re.compile("#%(\w+)%\s(.+)")
        tokenRegex = [RegexToken(self.__entity_name, Token.NAME, 2),
                      RegexToken(self.__entity_reexp, Token.ENTITY, 3, RegexEntity),
                      RegexToken(self.__entity_string, Token.ENTITY, 3, StringEntity),
                      RegexToken(self.__r_reexp, Token.ENTITY, 1, RegexEntity),
                      RegexToken(self.__r_order, Token.ENTITY, 1),
                      RegexToken(self.__r_entity, Token.ENTITY, 2),
                      RegexToken(self.__r_string, Token.ENTITY, 2, StringEntity),
                      RegexToken(self.__r_minus, Token.MINUS, 2),
                      RegexToken(self.__r_colon, Token.COLON, 1),
                      RegexToken(self.__r_repeat, Token.REPEAT, 2),
                      RegexToken(self.__r_bar, Token.BAR, 1),
                      RegexToken(self.__r_bar2, Token.BAR, 1),
                      RegexToken(self.__r_semicolon, Token.END, 1),
                      RegexToken(self.__r_conds, Token.ENTITY, 1, ScriptEntity),
                      ]
        sb = ""
        realRules = []
        rules = [x.strip() for x in text.split('\n')]  # PreProcessing
        for rule in rules:
            if propertyregex.match(rule):
                realRules.append(rule.strip())
                continue
            if rule.startswith("#"):
                sb = ""
                continue
            if rule.endswith(';'):
                sb += rule
                realRules.append(sb.strip())
                sb = ""
                continue
            else:
                sb += rule
        properties = {};
        for rule in realRules:
            m = propertyregex.match(rule);
            if m is not None:
                if m.lastindex is not None and m.lastindex == 2:
                    name, value = m.group(1), m.group(2);
                    if name == "Script":
                        item = __import__(value)
                        setattr(self, value, item)
                    elif name == "Include":
                        value = value.split(' ');
                        isadd = False;
                        if len(value) > 1:
                            isadd = value[1] == "True";
                        self.InitTNRule(value[0], isadd);
                    else:
                        properties[m.group(1)] = m.group(2);
                continue

            tokenItems = []  # Lexical Analyse
            while True:
                if len(rule) == 0:  break
                canmatch = False;
                for token in tokenRegex:
                    mat = token.Regex.match(rule)
                    if mat is None: continue

                    mcount = mat.lastindex if mat.lastindex is not None else 1;
                    if mcount < token.Count - 1:
                        continue
                    canmatch = True;
                    tokenItem = TokenItem(token.Token)
                    for r in range(mcount):
                        tokenItem.Values.append(mat.string if mat.lastindex is None else mat.group(r + 1))
                    tokenItem.Rule = mat.group(0)

                    e = None
                    if token.EntityType is not None:
                        e = token.EntityType()
                        e.Core = self
                        e.SetValues(tokenItem.Values)
                    elif token.Regex == self.__r_entity:
                        e = tokenItem.Values[0]
                    if e is not None:
                        tokenItem.Entity = e
                    rule = rule[len(tokenItem.Rule):]
                    tokenItems.append(tokenItem)
                    break
                if not canmatch:
                    print("rule format error%s" % (rule));
                    return;

            if Token.NAME != tokenItems[0].Token:  # Grammer Analyse
                print("name must be the first")
            if tokenItems[-1].Token != Token.END:
                print("Rule must be ended by ;")

            if findany(tokenItems, lambda r: r.Token == Token.BAR):
                entity = TableEntity()
                entity.Core = self;

                lastid = 0
                for id in range(1, len(tokenItems)):
                    if tokenItems[id].Token == Token.BAR or tokenItems[id].Token == Token.END:
                        tentity = self.__GetNonTableEntity(tokenItems[lastid + 1:id], isOnlyOne=False);
                        if isinstance(tentity, EntityBase) and tentity.Name == "":
                            tentity.Name = "%s_%d" % (tokenItems[0].Values[0], len(entity.Tables));
                        entity.Tables.append(tentity);
                        lastid = id
                        if tokenItems[id].Rule.find("/") == 0:
                            entity.Group.append(len(entity.Tables))
            else:
                entity = self.__GetNonTableEntity(tokenItems[1:-1], isOnlyOne=True)
            entity.Name = tokenItems[0].Values[0]

            entity.SetValues(properties);
            if entity.order != 0 and addtoorder:
                self.Entities.appendids(entity);
            properties = {};
            self.Entities.append(entity)

    def __GetNonTableEntity(self, tokenItems, isOnlyOne):
        repeat = getindex(tokenItems, lambda r: r.Token == Token.REPEAT)
        if repeat < 0:
            pass
        elif repeat != 1:
            raise Exception("repeat format error");
        else:
            entity = RepeatEntity()
            entity.Core = self
            entity.Entity = tokenItems[0].Entity
            entity.SetValues(tokenItems[1].Values)
            return entity
        minus = getindex(tokenItems, lambda r: r.Token == Token.MINUS)
        if minus < 0:
            pass
        elif minus != 1:
            raise Exception("diff format error");
        else:
            entity = DiffEntity()
            entity.Core = self
            entity.Universe = tokenItems[0].Entity
            id = 1
            while id < len(tokenItems):
                tokenItem = tokenItems[id]
                if tokenItem.Token == Token.END:
                    return entity
                if tokenItem.Token == Token.MINUS:
                    entity.Complements.append(tokenItems[id + 1].Entity)
                id += 1
            return entity
        if len(tokenItems) == 1:
            if not isOnlyOne:
                return tokenItems[0].Entity
            if isinstance(tokenItems[0].Entity, EntityBase) and tokenItems[0].Entity.Name == "":
                return tokenItems[0].Entity
        entity = SequenceEntity()
        entity.Core = self;
        state = 0
        for id in range(len(tokenItems)):
            tokenItem = tokenItems[id]
            if tokenItem.Token == Token.END:
                return entity
            if tokenItem.Token == Token.COLON:
                state += 1
                continue
            if state == 0:
                entity.MatchEntities.append(tokenItem.Entity)
            elif state == 1:
                if tokenItem.Entity is None:
                    entity.Rewriteorders.append(int(tokenItem.Rule.replace("$", "")) - 1)
                else:
                    entity.RewriteEntities.append(tokenItem.Entity)
                    entity.Rewriteorders.append(len(entity.Rewriteorders))
            else:
                entity.Condition = tokenItem.Entity
        return entity

    def InitTNRule(self, myfile, addtoorder=True):
        self.tnFileName = myfile
        if PY2:
            import codecs
            file_object = codecs.open(myfile, 'r', 'utf-8');
        else:
            file_object = open(myfile, 'r', encoding='utf-8')
        texts = file_object.read()
        print("success load tn rules:%s" % (myfile))
        self.InitRuleText(texts, addtoorder)
        file_object.close()

    def MatchEntity(self, entity, input, mode=None):
        startPos = 0
        matchResults = [];
        inputlen = len(input)
        while (1):
            if startPos >= inputlen:
                break
            matchResult = entity.MatchItem(input, startPos, None, entity.Start, mode)
            if IsFail(matchResult):
                if mode is not None and startPos == 0 and RegexCore.AutoModeStudy:
                    matchResult = entity.MatchItem(input, startPos, entity.Start)
                    if matchResult is not None:
                        self.__GetPublicTree(mode, matchResult)
                    else:
                        startPos = matchResult;
                break

            startPos = matchResult.pos + len(matchResult.match);
            matchResults.append(matchResult);
        return matchResults;

    def RewriteEntity(self, entity, input, mode=None):
        matchResults = self.MatchEntity(entity, input, mode);
        if len(matchResults) == 0:
            return input, False;
        else:
            pos = 0;
            rewrite = "";
            for m in matchResults:
                m.GetShouldRewrite();
                m.RewriteItem()
                rewrite += input[pos:m.pos] + m.rewrite;
                pos = m.pos + len(m.match);
            rewrite += input[pos:];
            return rewrite, True;

    def __GetPublicTree(self, item1, item2):
        if item1 is None:
            return item2
        stack1 = []
        stack2 = []
        stack1.append(item1)
        stack2.append(item2)
        while len(stack1) > 0:
            m1 = stack1.pop()
            m2 = stack2.pop()
            if m1.mindex != m2.mindex:
                m1.mindex = -1
                continue
            if isinstance(m1.children, EntityBase):
                continue
            m1 = m1.children
            m2 = m2.children
            while m1 != None:
                stack1.append(m1)
                stack2.append(m2)
                m1 = m1.NextMatch
                m2 = m2.NextMatch
        return item1

    def CompileString(self, input, modes):

        startPos = 0
        while (1):
            if startPos >= len(input):
                break
            modeindex = -1;
            matchResult = None;
            issuccess = False;
            if modes is not None:
                for index in range(0, len(modes)):
                    mode = modes[index];
                    matchResult = mode.Entity.MatchItem(input, startPos, entity.Start, mode);
                    if matchResult is not None:
                        modeindex = index;
                        issuccess = True;
                        break;
            if not issuccess:
                for entity in self.Entities.ValidEntities:
                    matchResult = entity.MatchItem(input, startPos, None, entity.Start)
                    if matchResult is not None:
                        if modes is None:
                            modes = [];
                        modes.append(matchResult);
                        break;
            if matchResult is None:
                return modes;
            if modes is not None and modeindex != -1:
                modes[modeindex] = self.__GetPublicTree(matchResult, modes[modeindex]);
            startPos += len(matchResult.match);
        return modes

    def Compile(self, texts):
        modes = None;
        for text in texts:
            modes = self.CompileString(text, modes);
        return modes;

    def Rewrite(self, rawinput, mode=None):

        if mode is not None:
            self.Entities.SeqBuff = BuffHelper(len(rawinput));
            return self.RewriteEntity(mode.Entity, rawinput, mode)
        else:
            self.Entities.SeqBuff = BuffHelper(len(rawinput));
            for entity in self.Entities.ValidEntities:
                rewrite, succ = self.RewriteEntity(entity, rawinput, None)
                if RegexCore.MatchAllEntity == False and succ == True:
                    return rewrite;
                if rewrite != rawinput:
                    rawinput = rewrite;
                    self.Entities.SeqBuff = BuffHelper(len(rawinput));
            return rewrite

    def Match(self, rawinput, mode=None):

        if mode is not None:
            self.Entities.SeqBuff = BuffHelper(len(rawinput));
            return self.MatchEntity(mode.Entity, rawinput, mode)
        else:
            self.Entities.SeqBuff = BuffHelper(len(rawinput));
            for entity in self.Entities.ValidEntities:
                match = self.MatchEntity(entity, rawinput, None)
                if not RegexCore.MatchAllEntity:
                    return match;
            return None;

    def __MatchResult2Doc__(self, matchResult):
        docu = {};
        matchResult.RewriteItem();
        matchResult.ExtractDocument(docu, 0);
        docu['#type'] = matchResult.entity.Name;
        docu['#pos'] = matchResult.pos;
        docu['#match'] = matchResult.match;
        docu['#rewrite'] = matchResult.rewrite;
        return docu

    def ExtractEntity(self, entity, input, mode=None):
        start = 0
        docs = [];
        buffhelper = self.Entities.SeqBuff;
        inputlen = len(input)
        while (1):
            if start >= inputlen:
                break;
            start = IsInExtractArea(buffhelper.extractedarea, start);
            matchResult = buffhelper.GetMatch(entity, input, start, None)

            if matchResult is None:
                matchResult = entity.MatchItem(input, start, None, entity.Start, mode)

            if IsFail(matchResult):
                if matchResult == start:
                    start = matchResult + 1;
                else:
                    start = matchResult;
                continue;

            p = IsInExtractArea(buffhelper.extractedarea, matchResult.pos);
            buffhelper.AddEntity(entity, matchResult)
            start = matchResult.pos + len(matchResult.match);
            if len(matchResult.match) == 0:
                start += 1;
            if p == matchResult.pos:
                docu = self.__MatchResult2Doc__(matchResult);
                docs.append(docu);
                buffhelper.AddScan(0, matchResult.pos, start);
        return docs;

    def Extract(self, input, modes=None, entities=None):
        if entities is None:
            entities = self.Entities.ValidEntities;
        self.Entities.SeqBuff = BuffHelper(len(input));
        docs = [];
        succ = False;
        if modes is not None:
            for mode in modes:
                entity = mode.Entity;
                mdocs = self.ExtractEntity(entity, input, mode)
                for doc in mdocs:
                    docs.append(doc);
                succ = True;
                break;
        if not succ:
            for entity in entities:
                mdocs = self.ExtractEntity(entity, input)
                for doc in mdocs:
                    docs.append(doc);
        return docs;


