"""
Generates information for supporting completion and analysis of Python code.

Outputs a pickled set of dictionaries.  The dictionaries are in the format:

top-level: module_table
        
module_table:
    {
        'members': {},             # member_table
        'doc': doc_string,         # doc string
    }

type_table:
   {
    'mro' :  list of type_name,     
    'bases' : list of type_name,
    'members' : member_table,
    'doc' : doc_string,
    'is_hidden': bool,
    'builtin': bool
   }

member_table:
    {member_name : member_entry}


member_name: str

member_entry:
    {
        'kind': member_kind
        'value': member_value

    }

member_kind: 'function' | 'method' | 'property' | 'data' | 'type' | 'multiple' | 'typeref' | 'moduleref'
member_value: builtin_function | getset_descriptor | data | type_table | multiple_value | typeref | moduleref

moduleref:
    {'module_name' : name }

typeref: 
    {'type_name' : type_name }

multiple_value:
    { 'members' : (member_entry, ... ) }

builtin_function:
    {'doc': doc string,
     'overloads': overload_table,
     'builtin' : bool,
     'static": bool,
     }

overload_table:
    (overload, ...)

overload:
    {'args': args_info,
     'ret_type': type_name }

args_info:
    [arg_info, ...]

arg_info:
    {'type': type_name,
     'name': argument name,
     'default_value': repr of default value,
     'arg_format' : ('*' | '**')
    }

getset_descriptor:
    {'doc': doc string,
     'type': type_name
    }

data:
    {'type': type_name}

type_name:
    (module name, type name)
    
"""

import types
try:
    import cPickle
except ImportError:
    import pickle as cPickle # Py3k

import sys
import os
import datetime

if sys.platform == "cli":
	# provides extra type info when generating against IronPython which can be used w/ CPython completions
    import IronPythonScraper as BuiltinScraper 
else:
	import BuiltinScraper


def type_to_name(type):
    return type.__module__, type.__name__
    
def generate_builtin_function(function):
    function_table = {}
    
    if isinstance(function.__doc__, str):
        function_table['doc'] = function.__doc__

    function_table['overloads'] = BuiltinScraper.get_overloads(function)
    
    return function_table
    
def generate_getset_descriptor(descriptor):
    descriptor_table = {}
    
    if isinstance(descriptor.__doc__, str):
        descriptor_table['doc'] = descriptor.__doc__
    
    desc_type = BuiltinScraper.get_descriptor_type(descriptor)
    descriptor_table['type'] = type_to_name(desc_type)
    
    return descriptor_table

slot_wrapper_type = type(int.__add__)
method_descriptor_type = type(str.center)
member_descriptor_type = type(property.fdel)
try:
    getset_descriptor_type = type(file.closed)
except NameError:
    getset_descriptor_type = type(Exception.args) # Py3k, no file

class_method_descriptor_type = type(datetime.date.__dict__['today'])
class OldStyleClass: pass
OldStyleClassType = type(OldStyleClass)

def generate_member(obj, is_hidden=False):
    member_table = {}
    
    if isinstance(obj, (types.BuiltinFunctionType, class_method_descriptor_type)):
        member_table['kind'] = 'function'
        member_table['value'] = generate_builtin_function(obj)
    elif isinstance(obj, (type, OldStyleClassType)):
        member_table['kind'] = 'type'        
        member_table['value'] = generate_type(obj, is_hidden=is_hidden)
    elif isinstance(obj, (types.BuiltinMethodType, slot_wrapper_type, method_descriptor_type)):
        member_table['kind'] = 'method'        
        member_table['value'] = generate_builtin_function(obj)
    elif isinstance(obj, (getset_descriptor_type, member_descriptor_type)):
        member_table['kind'] = 'property'        
        member_table['value'] = generate_getset_descriptor(obj)
    else:
        member_table['kind'] = 'data'
        member_table['value'] = generate_data(obj)
        
    return member_table
    
def oldstyle_mro(type_obj, res):
    for base in type_obj.__bases__:
        if base not in res:
            res.append(base)

    for base in type_obj.__bases__:
        oldstyle_mro(base, res)
    return res

def generate_type(type_obj, is_hidden=False):
    type_table = {}
    
    if hasattr(type_obj, '__mro__'):
        type_table['mro'] = [type_to_name(mro_type) for mro_type in type_obj.__mro__]
    else:
        type_table['mro'] = oldstyle_mro(type_obj, [])

    type_table['bases'] = [type_to_name(mro_type) for mro_type in type_obj.__bases__]
    type_table['members'] = members_table = {}
    
    if isinstance(type_obj.__doc__, str):
         type_table['doc'] = type_obj.__doc__

    if is_hidden:
        type_table['is_hidden'] = True
    
    for member in type_obj.__dict__:
        if type_obj is object and member == '__new__':
            members_table[member] = {'kind' : 'function', 'value': { 'overloads': ({'args': [{'name': 'cls', 'type': (builtin_name, 'type'), 'ret_type': (builtin_name, 'object')}]})}}
        else:
            members_table[member] = generate_member(type_obj.__dict__[member])
    
    return type_table

def generate_data(data_value):
    data_table = {}
    
    data_type = type(data_value)
    data_table['type'] = type_to_name(data_type)
    
    return data_table

def generate_module(module_name):
    module = __import__(module_name)    
    all_members = {}
    module_table = {'members': all_members}
        
    if isinstance(module.__doc__, str):
        module_table['doc'] = module.__doc__

    for attr in module.__dict__:
        attr_value = module.__dict__[attr]
        
        all_members[attr] = generate_member(attr_value)
            
    return module_table



if sys.version_info[0] == 2:
    builtin_name = '__builtin__'
else:
    builtin_name = 'builtins'

def generate_builtin_module():
    res  = generate_module(builtin_name)

    # add some hidden members we need to support resolving to
    members_table = res['members']
    
    members_table['function'] = generate_member(types.FunctionType, is_hidden=True)
    members_table['builtin_function'] = generate_member(types.BuiltinFunctionType, is_hidden=True)
    members_table['builtin_method_descriptor'] = generate_member(types.BuiltinMethodType, is_hidden=True)
    members_table['generator'] = generate_member(types.GeneratorType, is_hidden=True)
    members_table['NoneType'] = generate_member(type(None), is_hidden=True)
    members_table['ellipsis'] = generate_member(type(Ellipsis), is_hidden=True)
    
    return res


def merge_type(baseline_type, new_type):
    #if 'doc' not in new_type and 'doc' in baseline_type:
    #    print 'moved doc over', baseline_type['doc']
    #    new_type['doc'] = baseline_type['doc']

    merge_member_table(baseline_type['members'], new_type['members'])
    
    return new_type

def merge_function(baseline_func, new_func):
    return new_func

def merge_property(baseline_prop, new_prop):
    if new_prop['type'] != baseline_prop['type']:
        if new_prop['type'] == (builtin_name, 'object'):
            new_prop['type'] = baseline_prop['type']
            #print 'property different types', new_prop['type'], baseline_prop['type']

    return new_prop

def merge_data(baseline_data, new_data):
    if new_data['type'] != baseline_data['type']:
        if new_data['type'] == (builtin_name, 'object'):
            new_data['type'] = baseline_data['type']
            #print 'data different types', new_data['type'], baseline_data['type']

    return new_data

def merge_method(baseline_method, new_method):
    if 'overloads' in baseline_method and ('overloads' not in new_method or new_method['overloads'] is None):
        #print 'merging method', new_method
        new_method['overloads'] = baseline_method['overloads']
    elif 'overloads' in new_method:
        print('has overloads', new_method['overloads'])
    
    if 'doc' in baseline_method and 'doc' not in new_method:
        new_method['doc'] = baseline_method['doc']
        #print 'new doc string'

    return new_method

_MERGES = {'type' : merge_type,
          'function': merge_method,
          'property': merge_property,
          'data': merge_data,
          'method': merge_method}

def merge_member_table(baseline_table, new_table):
    for name, member_table in new_table.items():
        base_member_table = baseline_table.get(name, None)        
        kind = member_table['kind']
        
        if base_member_table is not None and base_member_table['kind'] == kind:
            merger = _MERGES.get(kind, None)
            if merger is not None:
                member_table['value'] = merger(base_member_table['value'], member_table['value'])
            else:
                print('unknown kind')
        elif base_member_table is not None:
            print('kinds differ', kind, base_member_table['kind'], name)
    
def merge_with_baseline(mod_name, baselinepath, final):
    if baselinepath is not None:
        baseline_file = os.path.join(baselinepath, mod_name + '.idb')
        if os.path.exists(baseline_file):
            print(baseline_file)
            f = open(baseline_file, 'rb')
            baseline = cPickle.load(f)
            f.close()

            #import pprint
            #pp = pprint.PrettyPrinter()
            #pp.pprint(baseline['members'])

            merge_member_table(baseline['members'], final['members'])

    return final

if __name__ == "__main__":
    outpath = sys.argv[1]
    if len(sys.argv) > 2:
        baselinepath = sys.argv[2]
    else:
        baselinepath = None
    
    res = generate_builtin_module()
    
    #import pprint
    #pp = pprint.PrettyPrinter()
    #pp.pprint(res['members']['NoneType'])

    res = merge_with_baseline(builtin_name, baselinepath, res)

    cPickle.dump(res, open(os.path.join(outpath, builtin_name + '.idb'), 'wb'), 2)
    
    for mod_name in sys.builtin_module_names:
        if mod_name == builtin_name or mod_name == '__main__': continue
        
        res = generate_module(mod_name)
        
        try:
            res = merge_with_baseline(mod_name, baselinepath, res)

            cPickle.dump(res, open(os.path.join(outpath, mod_name + '.idb'), 'wb'), 2)        
        except ValueError:
            pass
