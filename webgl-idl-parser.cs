using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Irony.Parsing;

using Argument = System.Collections.Generic.KeyValuePair<string,string>;
using Enum = System.Collections.Generic.KeyValuePair<string,string>;

namespace WebIDLSharp
{
	public class WebGLIDLParserDriver
	{
		public static void Main (string [] args)
		{
			new WebGLIDLParserDriver ().Run (args);
		}
		
		string src;

		void Run (string [] args)
		{
			var dic = new Dictionary<string,AstNodeCreator> ();
			dic ["DelimitedComment"] = CreateCommentNode;
			dic ["Identifier"] = CreateIdentifierNode;
//			dic ["ReturnType"] = CreateReturnTypeNode;
			dic ["Interface"] = CreateInterfaceNode;
			dic ["Const"] = CreateConstNode;
			dic ["Operation"] = CreateOperationNode;
			dic ["OperationRest"] = CreateOperationRestNode;
			dic ["*"] = NotImplemented;

			var grammar = new WebGLWebIDLGrammar (dic);
			var parser = new Parser (grammar);

			foreach (var arg in args) {
				src = File.ReadAllText (arg);
				var pt = parser.Parse (src, arg);
				foreach (var msg in pt.ParserMessages)
					Console.Error.WriteLine ("{0} {1} {2} {3}", msg.Level, msg.ParserState, msg.Location, msg.Message);
				if (pt.ParserMessages.Count > 0)
					break;

				grammar.CreateAstNode (parser.Context, pt.Root);

				//foreach (var p in enum_categories)
				//	Console.WriteLine ("[enum] {0}: {1}", p.Value, p.Key);
				Console.WriteLine ("<signatures>");
				Console.WriteLine ("  <add>");
				foreach (var p in from e in enums orderby e.Key select e) {
					Console.WriteLine ("    <enum name='{0}' type='int'>".Replace ('\'', '"'), CodeIdentifier.MakePascal (p.Key));
					foreach (var t in from x in p.Value orderby x.Key select x)
						Console.WriteLine ("      <token name='{0}' value='{1}' />".Replace ('\'', '"'), t.Key, t.Value);
					Console.WriteLine ("    </enum>");
				}
				foreach (var f in from x in functions orderby x.Name select x)
					Console.Write (f);
				Console.WriteLine ("  </add>");
				Console.WriteLine ("</signatures>");
			}
		}

		void NotImplemented (ParsingContext ctx, ParseTreeNode node)
		{
//Console.WriteLine ("Node {0} has {1} children", node.Term.Name, node.ChildNodes.Count);
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (ctx, cn);

//Console.WriteLine (node.Term.Name + " :: " + node.Token + " // " + node.ChildNodes.Count);
			if (node.Token != null)
				node.AstNode = node.Token.Value ?? node.Term.Name;
			else {
				if (node.ChildNodes.Any (cn => cn.AstNode != null))
					node.AstNode = (from cn in node.ChildNodes select cn.AstNode).ToArray ();
				else
					node.AstNode = null;
			}
		}

		void CreateInterfaceNode (ParsingContext context, ParseTreeNode node)
		{
			current_enum_category = null;
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (context, cn);
		}

		string current_enum_category;
		Dictionary<string,string> enum_categories = new Dictionary<string,string> ();
		List<Function> functions = new List<Function> ();
		Regex frex = new Regex (@"([\w\[\]]+)\s(\w+)\s*\((.*)\s*\)\s*;");
		Regex erex = new Regex (@"const GLenum (\w+)\s*=\s* (\w+);");
		Dictionary<string,List<Enum>> enums = new Dictionary<string,List<Enum>> ();

		void CreateConstNode (ParsingContext context, ParseTreeNode node)
		{
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (context, cn);

			var comm = node.ChildNodes.FirstOrDefault (n => n.Term.Name == "might-be-significant comment" && n.ChildNodes.Count > 0);
			var cat = comm != null ? comm.Get<string> (0).Trim () : null;
			if (cat != null) {
				cat = cat.Substring (2).Substring (0, cat.Length - 4).Trim (); // trim /* and */
				current_enum_category = cat;
			}
			enum_categories [(string) node.ChildNodes [3].AstNode] = current_enum_category;

			var def = src.Substring (node.Span.Location.Position, node.Span.Length).Replace ("\r\n", "\n");
			var m = erex.Match (def);
			if (!enums.ContainsKey (current_enum_category))
				enums [current_enum_category] = new List<Enum> ();
			enums [current_enum_category].Add (new Enum (m.Groups [1].ToString ().Trim (), m.Groups [2].ToString ().Trim ()));
		}

		class Function
		{
			public string Name;
			public string ReturnType;
			public List<Argument> Arguments = new List<Argument> ();

			public override string ToString ()
			{
				var sw = new StringWriter ();
				sw.WriteLine ("    <function name='{0}' extension='Core' profile='' category='2.0' version='2.0'>".Replace ('\'', '"'), CodeIdentifier.MakePascal (Name));
				sw.WriteLine ("      <returns type='{0}' />".Replace ('\'', '"'), ReturnType);
				foreach (var p in Arguments)
					sw.WriteLine ("      <param type='{0}' name='{1}' flow='in' />".Replace ('\'', '"'), p.Key.Replace ("<", "&lt;").Replace (">", "&gt;"), p.Value);
				sw.WriteLine ("    </function>");
				return sw.ToString ();
			}
		}

		void CreateOperationNode (ParsingContext context, ParseTreeNode node)
		{
			var def = src.Substring (node.Span.Location.Position, node.Span.Length).Replace ("\r\n", "\n");
			def = String.Join ("", (from s in def.Split ('\n') select s.Trim ()).ToArray ());
			def = def.Replace ("[ ]", "[]"); // hack!
			var m = frex.Match (def);

			var f = new Function () { ReturnType = m.Groups [1].ToString (), Name = m.Groups [2].ToString () };

			var c = new char [] {','};
			foreach (var s in m.Groups [3].ToString ().Split (c, StringSplitOptions.RemoveEmptyEntries)) {
				var pair = s.Trim ().Split (' ');
				f.Arguments.Add (new Argument (pair [0].Trim (), pair [1].Trim ()));
			}
			
			functions.Add (f);

			/*
			current_enum_category = null;
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (context, cn);

			var q = node.GetNullable<object> (0);
			if (q != null)
				throw new Exception (String.Format ("In operation {0}, qualifiers not supported: {1}", node.ChildNodes [1].ChildNodes [1].AstNode, q));
//Console.WriteLine (String.Join (" ", (from cn in node.ChildNodes where cn.AstNode != null select cn.Term.Name + ":" + cn.AstNode.ToString ()).ToArray ()));
			*/
		}

		class OperationRest
		{
			public object ReturnType;
			public object Name;
			public object Arguments;
			public object Raises;
			
			public override string ToString ()
			{
				return String.Format ("{0} {1} ({2}) {3} {4}", ReturnType, Name, Arguments, Raises != null ? "raises" : null, Raises);
			}
		}

		void CreateOperationRestNode (ParsingContext context, ParseTreeNode node)
		{
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (context, cn);
			var or = new OperationRest () {
				ReturnType = node.GetNullable<object> (0),
				Name = node.GetNullable<object> (1),
				Arguments = node.GetNullable<object> (3),
				Raises = node.GetNullable<object> (5) };
			node.AstNode = or;
		}

		static void CreateReturnTypeNode (ParsingContext context, ParseTreeNode node)
		{
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (context, cn);
//Console.WriteLine ("Children: {0} Token {1}", node.ChildNodes.Count, node.Token);
//foreach (var cn in node.ChildNodes) Console.WriteLine ("-> " + cn.AstNode);
		}

		static void CreateIdentifierNode (ParsingContext context, ParseTreeNode node)
		{
			node.AstNode = node.Token.Value;
		}

		void CreateCommentNode (ParsingContext context, ParseTreeNode node)
		{
			node.AstNode = node.Token.Value;
		}
	}

	static class IronyExtensions
	{
		public static T Get<T> (this ParseTreeNode node, int index)
		{
			var x = node.ChildNodes [index].AstNode;
			if (x == null)
				throw new InvalidOperationException (String.Format ("Node {0} child {1} has null AstNode ({2})", node.Term.Name, index, node.Span.Location));
			return (T) x;
		}

		public static T GetNullable<T> (this ParseTreeNode node, int index)
		{
			return (T) node.ChildNodes [index].AstNode;
		}
	}
}
