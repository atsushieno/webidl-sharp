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
			dic ["Const"] = CreateConstNode;
			dic ["Operation"] = CreateOperationNode;
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
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (ctx, cn);

			if (node.Token != null)
				node.AstNode = node.Token.Value;
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

		Regex frex = new Regex (@"([\w\[\]]+)\s(\w+)\s*\((.*)\s*\)\s*;");
		Regex erex = new Regex (@"const GLenum (\w+)\s*=\s* (\w+);");
		string current_enum_category;
		List<Function> functions = new List<Function> ();
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
