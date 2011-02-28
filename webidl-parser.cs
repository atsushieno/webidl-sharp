using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Irony.Parsing;

namespace WebIDLSharp
{
	[Language ("WebIDL(WebGL)", "WD20110201", "Web IDL grammar for WebGL")]
	public partial class WebGLWebIDLGrammar : WebIDLGrammar
	{
		public WebGLWebIDLGrammar ()
			: this (null)
		{
		}

		public WebGLWebIDLGrammar (Dictionary<string,AstNodeCreator> astActionMap)
			: base (true, astActionMap)
		{
		}
	}
	
	[Language ("WebIDL", "WD20110201", "Web IDL grammar")]
	public partial class WebIDLGrammar : Grammar
	{
		Dictionary<string,AstNodeCreator> ast_action_map;
		
		KeyTerm Keyword (string label)
		{
			return ToTerm (label);
		}

		void ApplyCreator (BnfTerm ret)
		{
			string label = ret.Name;
			if (ast_action_map != null && ast_action_map.ContainsKey (label))
				ret.AstNodeCreator = ast_action_map [label];
			else if (ast_action_map != null && ast_action_map.ContainsKey ("*"))
				ret.AstNodeCreator = ast_action_map ["*"];
		}
		
		NonTerminal CreateNonTerminal (string label)
		{
			var ret = new NonTerminal (label);
			ApplyCreator (ret);
			return ret;
		}
		
		public WebIDLGrammar ()
			: this (false, null)
		{
		}
		
		protected WebIDLGrammar (bool webglSpecific, Dictionary<string,AstNodeCreator> astActionMap)
		{
			ast_action_map = astActionMap;

var single_line_comment = new CommentTerminal ("SingleLineComment", "//", "\r", "\n");
ApplyCreator (single_line_comment);
var delimited_comment = new CommentTerminal ("DelimitedComment", "/*", "*/");
ApplyCreator (delimited_comment);

NonGrammarTerminals.Add (single_line_comment);
if (!webglSpecific)
	NonGrammarTerminals.Add (delimited_comment);

// FIXME: should be generic identifiers or its own.
IdentifierTerminal identifier = new IdentifierTerminal ("Identifier");
ApplyCreator (identifier);
StringLiteral string_literal = new StringLiteral ("StringLiteral", "\"");
NumberLiteral integer_literal = new NumberLiteral ("Integer") { Options = NumberOptions.IntOnly | NumberOptions.AllowSign | NumberOptions.Hex };
NumberLiteral float_literal = TerminalFactory.CreateCSharpNumber ("Float");
var other_literal = new RegexBasedTerminal ("[^\t\n\r 0-9A-Z_a-z]");

var definitions = CreateNonTerminal ("Definitions");
var x_definition = CreateNonTerminal ("x_definition");
var definition = CreateNonTerminal ("Definition");
var module = CreateNonTerminal ("Module");
var interface_ = CreateNonTerminal ("Interface");
var interface_inheritance = CreateNonTerminal ("InterfaceInheritance");
var interface_members = CreateNonTerminal ("InterfaceMembers");
var x_interface_member = CreateNonTerminal ("x_interface_member");
var interface_member = CreateNonTerminal ("InterfaceMember");
var exception = CreateNonTerminal ("Exception");
var exception_members = CreateNonTerminal ("ExceptionMembers");
var typedef = CreateNonTerminal ("Typedef");
var implements_statement = CreateNonTerminal ("ImplementsStatement");
var const_ = CreateNonTerminal ("Const");
var const_expr = CreateNonTerminal ("ConstExpr");
var boolean_literal = CreateNonTerminal ("BooleanLiteral");
var attribute_or_operation = CreateNonTerminal ("AttributeOrOperation");
var stringified_att_oper = CreateNonTerminal ("stringified_attr_or_oper");
var stringifier_attribute_or_operation = CreateNonTerminal ("StringifierAttributeOrOperation");
var attribute = CreateNonTerminal ("Attribute");
var readonly_ = CreateNonTerminal ("ReadOnly");
var get_raises = CreateNonTerminal ("GetRaises");
var set_raises = CreateNonTerminal ("SetRaises");
var operation = CreateNonTerminal ("Operation");
var qualifiers = CreateNonTerminal ("Qualifiers");
var specials = CreateNonTerminal ("Specials");
var special = CreateNonTerminal ("Special");
var operation_rest = CreateNonTerminal ("OperationRest");
var optional_identifier = CreateNonTerminal ("OptionalIdentifier");
var raises = CreateNonTerminal ("Raises");
var exception_list = CreateNonTerminal ("ExceptionList");
var argument_list = CreateNonTerminal ("ArgumentList");
var arguments = CreateNonTerminal ("Arguments");
var argument = CreateNonTerminal ("Argument");
var in_ = CreateNonTerminal ("In");
var optional = CreateNonTerminal ("Optional");
var ellipsis = CreateNonTerminal ("Ellipsis");
var exception_member = CreateNonTerminal ("ExceptionMember");
var exception_field = CreateNonTerminal ("ExceptionField");
var extended_attribute_list = CreateNonTerminal ("ExtendedAttributeList");
var extended_attributes = CreateNonTerminal ("ExtendedAttributes");
var extended_attribute = CreateNonTerminal ("ExtendedAttribute");
var extended_attribute_rest = CreateNonTerminal ("ExtendedAttributeRest");
var extended_attribute_inner = CreateNonTerminal ("ExtendedAttributeInner");
var other = CreateNonTerminal ("Other");
var other_or_comma = CreateNonTerminal ("OtherOrComma");
var type = CreateNonTerminal ("Type");
var nullable_type = CreateNonTerminal ("NullableType");
var unsigned_integer_type = CreateNonTerminal ("UnsignedIntegerType");
var integer_type = CreateNonTerminal ("IntegerType");
var optional_long = CreateNonTerminal ("OptionalLong");
var nullable = CreateNonTerminal ("Nullable");
var array = CreateNonTerminal ("Array");
var return_type = CreateNonTerminal ("ReturnType");
var scoped_name_list = CreateNonTerminal ("ScopedNameList");
var scoped_name = CreateNonTerminal ("ScopedName");
var absolute_scoped_name = CreateNonTerminal ("AbsoluteScopedName");
var relative_scoped_name = CreateNonTerminal ("RelativeScopedName");
var scoped_name_parts = CreateNonTerminal ("ScopedNameParts");
var extended_attribute_no_arg = CreateNonTerminal ("ExtendedAttributeNoArg");
var extended_attribute_arg_list = CreateNonTerminal ("ExtendedAttributeArgList");
var extended_attribute_ident = CreateNonTerminal ("ExtendedAttributeIdent");
var extended_attribute_scoped_name = CreateNonTerminal ("ExtendedAttributeScopedName");
var extended_attribute_named_arg_list = CreateNonTerminal ("ExtendedAttributeNamedArgList");

var delimited_comment_node = CreateNonTerminal ("might-be-significant comment");
delimited_comment_node.Rule = MakeStarRule (delimited_comment_node, null, delimited_comment);

definitions.Rule = MakeStarRule (definitions, null, x_definition);
if (webglSpecific)
	x_definition.Rule = delimited_comment_node + extended_attribute_list + definition;
else
	x_definition.Rule = extended_attribute_list + definition;
definition.Rule = module | interface_ | exception | typedef | implements_statement;
module.Rule = "module" + identifier + "{" + definitions + "}" + ";";
interface_.Rule = "interface" + identifier + interface_inheritance + "{" + interface_members + "}" + ";";
interface_inheritance.Rule = Empty | ":" + scoped_name_list;
interface_members.Rule = MakeStarRule (interface_members, null, x_interface_member);
x_interface_member.Rule = extended_attribute_list + interface_member;
interface_member.Rule = const_ | attribute_or_operation;
exception.Rule = "exception" + identifier + "{" + exception_members + "}" + ";";
exception_members.Rule = MakeStarRule (exception_members, null, extended_attribute_list + exception_member);
typedef.Rule = "typedef" + type + identifier + ";";
implements_statement.Rule = scoped_name + "implements" + scoped_name + ";";
if (webglSpecific)
	const_.Rule = delimited_comment_node + "const" + type + identifier + "=" + const_expr + ";";
else
	const_.Rule = "const" + type + identifier + "=" + const_expr + ";";
const_expr.Rule = boolean_literal | integer_literal | float_literal;
boolean_literal.Rule = Keyword ("true") | Keyword ("false");
attribute_or_operation.Rule = stringified_att_oper | attribute | operation;
stringified_att_oper.Rule = "stringifier" + stringifier_attribute_or_operation;
stringifier_attribute_or_operation.Rule = attribute | operation_rest + ";";
attribute.Rule = readonly_ + "attribute" + type + identifier + get_raises + set_raises + ";";
readonly_.Rule = Keyword ("readonly") | Empty;
get_raises.Rule = "getraises" + exception_list | Empty;
set_raises.Rule = "setraises" + exception_list | Empty;
operation.Rule = qualifiers + operation_rest;
qualifiers.Rule = "static" | "omittable" + specials | specials;
specials.Rule = MakeStarRule (specials, null, special);
special.Rule = Keyword ("getter") | Keyword ("setter") | Keyword ("creator") | Keyword ("deleter") | Keyword ("caller");
operation_rest.Rule = return_type + optional_identifier + "(" + argument_list + ")" + raises + ";";
optional_identifier.Rule = Empty | identifier;
raises.Rule = Empty | "raises" + exception_list;
exception_list.Rule = "(" + scoped_name_list + ")";
argument_list.Rule = MakeStarRule (argument_list, ToTerm (","), argument);
// arguments.Rule = "," + argument + arguments | Empty;
argument.Rule = extended_attribute_list + in_ + optional + type + ellipsis + identifier;
in_.Rule = Empty | "in";
optional.Rule = Empty | "optional";
ellipsis.Rule = Empty | "...";
exception_member.Rule = const_ | exception_field;
exception_field.Rule = type + identifier + ";";
extended_attribute_list.Rule = "[" + MakePlusRule (extended_attributes, ToTerm (","), extended_attribute) + "]" | Empty;
//extended_attributes.Rule = "," + extended_attribute + extended_attributes;
extended_attribute.Rule = 
	"(" + extended_attribute_inner + ")" + extended_attribute_rest
	| "[" + extended_attribute_inner + "]" + extended_attribute_rest
	| "{" + extended_attribute_inner + "}" + extended_attribute_rest
	| other + extended_attribute_rest;
extended_attribute_rest.Rule = Empty | extended_attribute;
extended_attribute_inner.Rule = 
	"(" + extended_attribute_inner + ")" + extended_attribute_inner
	| "[" + extended_attribute_inner + "]" + extended_attribute_inner
	| "{" + extended_attribute_inner + "}" + extended_attribute_inner
	| other_or_comma + extended_attribute_inner
	| Empty;
other.Rule = integer_literal | float_literal | identifier | string_literal | other_literal | "..." | ":" | "::" | ";" | "<" | "=" | ">" | "?" | "false" | "object" | "true" | "any" | "attribute" | "boolean" | "caller" | "const" | "creator" | "deleter" | "double" | "exception" | "float" | "getraises" | "getter" | "implements" | "in" | "interface" | "long" | "module" | "octet" | "omittable" | "optional" | "raises" | "sequence" | "setraises" | "setter" | "short" | "DOMString" | "stringifier" | "typedef" | "unsigned" | "void" | "static";
other_or_comma.Rule = other | ",";
type.Rule =
	nullable_type + array
	| scoped_name + array
	| "any" + array
	| "object" + array;
nullable_type.Rule = 
	unsigned_integer_type + nullable 
	| "boolean" + nullable
	| "octet" + nullable
	| "float" + nullable
	| "double" + nullable
	| "DOMString" + nullable
	| "sequence" + ToTerm ("<") + type + ">" + nullable;
unsigned_integer_type.Rule = "unsigned" + integer_type | integer_type;
integer_type.Rule = "short" | "long" + optional_long;
optional_long.Rule = "long" | Empty;
nullable.Rule = "?" | Empty;
array.Rule = ToTerm ("[") + "]" + array | Empty;
return_type.Rule = type | "void";
scoped_name_list.Rule = MakePlusRule (scoped_name_list, ToTerm (","), scoped_name);
//scoped_names.Rule = "," + scoped_name + scoped_names;
scoped_name.Rule = absolute_scoped_name | relative_scoped_name;
absolute_scoped_name.Rule = "::" + identifier + scoped_name_parts;
relative_scoped_name.Rule = identifier + scoped_name_parts;
scoped_name_parts.Rule = "::" + identifier + scoped_name_parts | Empty;
extended_attribute_no_arg.Rule = identifier;
extended_attribute_arg_list.Rule = identifier + "(" + argument_list + ")";
extended_attribute_ident.Rule = identifier + "=" + identifier;
extended_attribute_scoped_name.Rule = identifier + "=" + scoped_name;
extended_attribute_named_arg_list.Rule = identifier + "=" + identifier + "(" + argument_list + ")";

Root = definitions;
MarkPunctuation (";", ",", "{", "}", "[", "]", ":", "?");

MarkTransient (definition, const_expr, extended_attribute_rest, other, attribute_or_operation, interface_member, optional_identifier);
		}
	}
}
