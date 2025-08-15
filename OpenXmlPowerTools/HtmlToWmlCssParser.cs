using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Codeuctivity.OpenXmlPowerTools
{
    public class CssAttribute
    {
        public string Operand { get; set; }

        public CssAttributeOperator? Operator { get; set; } = null;

        public string? CssOperatorString
        {
            get
            {
                if (Operator.HasValue)
                {
                    return Operator.Value.ToString();
                }
                else
                {
                    return null;
                }
            }
            set => Operator = (CssAttributeOperator)Enum.Parse(typeof(CssAttributeOperator), value);
        }

        public string Value { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[{0}", Operand);
            if (Operator.HasValue)
            {
                switch (Operator.Value)
                {
                    case CssAttributeOperator.Equals:
                        sb.Append("=");
                        break;

                    case CssAttributeOperator.InList:
                        sb.Append("~=");
                        break;

                    case CssAttributeOperator.Hyphenated:
                        sb.Append("|=");
                        break;

                    case CssAttributeOperator.BeginsWith:
                        sb.Append("$=");
                        break;

                    case CssAttributeOperator.EndsWith:
                        sb.Append("^=");
                        break;

                    case CssAttributeOperator.Contains:
                        sb.Append("*=");
                        break;
                }
                sb.Append(Value);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }

    public enum CssAttributeOperator
    {
        Equals,
        InList,
        Hyphenated,
        EndsWith,
        BeginsWith,
        Contains,
    }

    public enum CssCombinator
    {
        ChildOf,
        PrecededImmediatelyBy,
        PrecededBy,
    }

    public class CssDocument : IRuleSetContainer
    {
        public IList<CssDirective> Directives { get; set; } = new List<CssDirective>();

        public IList<CssRuleSet> RuleSets { get; set; } = new List<CssRuleSet>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var cssDir in Directives)
            {
                sb.AppendFormat("{0}" + Environment.NewLine, cssDir.ToString());
            }
            if (sb.Length > 0)
            {
                sb.Append(Environment.NewLine);
            }
            foreach (var rules in RuleSets)
            {
                sb.AppendFormat("{0}" + Environment.NewLine,
                    rules.ToString());
            }
            return sb.ToString();
        }
    }

    public class CssDeclaration
    {
        public string Name { get; set; }

        public bool Important { get; set; }

        public CssExpression Expression { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}: {1}{2}",
                Name,
                Expression.ToString(),
                Important ? " !important" : "");
            return sb.ToString();
        }
    }

    public class CssDirective : IDeclarationContainer, IRuleSetContainer
    {
        public CssDirectiveType Type { get; set; }

        public string Name { get; set; }

        public CssExpression Expression { get; set; }

        public IList<CssMedium> Mediums { get; set; } = new List<CssMedium>();

        public IList<CssDirective> Directives { get; set; } = new List<CssDirective>();

        public IList<CssRuleSet> RuleSets { get; set; } = new List<CssRuleSet>();

        public IList<CssDeclaration> Declarations { get; set; } = new List<CssDeclaration>();

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int indentLevel)
        {
            var start = "".PadRight(indentLevel, '\t');

            switch (Type)
            {
                case CssDirectiveType.Charset:
                    return ToCharSetString(start);

                case CssDirectiveType.Page:
                    return ToPageString(start);

                case CssDirectiveType.Media:
                    return ToMediaString(indentLevel);

                case CssDirectiveType.Import:
                    return ToImportString();

                case CssDirectiveType.FontFace:
                    return ToFontFaceString(start);
            }

            var sb = new StringBuilder();

            sb.AppendFormat("{0} ", Name);

            if (Expression != null)
            {
                sb.AppendFormat("{0} ", Expression);
            }

            var first = true;
            foreach (var med in Mediums)
            {
                if (first)
                {
                    first = false;
                    sb.Append(" ");
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(med.ToString());
            }

            var HasBlock = Declarations.Count > 0 || Directives.Count > 0 || RuleSets.Count > 0;

            if (!HasBlock)
            {
                sb.Append(";");
                return sb.ToString();
            }

            sb.Append(" {" + Environment.NewLine + start);

            foreach (var dir in Directives)
            {
                sb.AppendFormat("{0}" + Environment.NewLine, dir.ToCharSetString(start + "\t"));
            }

            foreach (var rules in RuleSets)
            {
                sb.AppendFormat("{0}" + Environment.NewLine, rules.ToString(indentLevel + 1));
            }

            first = true;
            foreach (var decl in Declarations)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(";");
                }
                sb.Append(Environment.NewLine + "\t" + start);
                sb.Append(decl.ToString());
            }

            sb.Append(Environment.NewLine + "}");
            return sb.ToString();
        }

        private string ToFontFaceString(string start)
        {
            var sb = new StringBuilder();
            sb.Append("@font-face {");

            var first = true;
            foreach (var decl in Declarations)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(";");
                }
                sb.Append(Environment.NewLine + "\t" + start);
                sb.Append(decl.ToString());
            }

            sb.Append(Environment.NewLine + "}");
            return sb.ToString();
        }

        private string ToImportString()
        {
            var sb = new StringBuilder();
            sb.Append("@import ");
            if (Expression != null)
            {
                sb.AppendFormat("{0} ", Expression);
            }
            var first = true;
            foreach (var med in Mediums)
            {
                if (first)
                {
                    first = false;
                    sb.Append(" ");
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(med.ToString());
            }
            sb.Append(";");
            return sb.ToString();
        }

        private string ToMediaString(int indentLevel)
        {
            var sb = new StringBuilder();
            sb.Append("@media");

            var first = true;
            foreach (var medium in Mediums)
            {
                if (first)
                {
                    first = false;
                    sb.Append(" ");
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(medium.ToString());
            }
            sb.Append(" {" + Environment.NewLine);

            foreach (var ruleset in RuleSets)
            {
                sb.AppendFormat("{0}" + Environment.NewLine, ruleset.ToString(indentLevel + 1));
            }

            sb.Append("}");
            return sb.ToString();
        }

        private string ToPageString(string start)
        {
            var sb = new StringBuilder();
            sb.Append("@page ");
            if (Expression != null)
            {
                sb.AppendFormat("{0} ", Expression);
            }
            sb.Append("{" + Environment.NewLine);

            var first = true;
            foreach (var decl in Declarations)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(";");
                }
                sb.Append(Environment.NewLine + "\t" + start);
                sb.Append(decl.ToString());
            }

            sb.Append("}");
            return sb.ToString();
        }

        private string ToCharSetString(string start)
        {
            return string.Format("{2}{0} {1}",
                Name,
                Expression.ToString(),
                start);
        }
    }

    public enum CssDirectiveType
    {
        Media,
        Import,
        Charset,
        Page,
        FontFace,
        Namespace,
        Other,
    }

    public class CssExpression
    {
        public List<CssTerm> Terms { get; set; } = new List<CssTerm>();

        public bool IsNotAuto => this != null && ToString() != "auto";

        public bool IsAuto => this != null && ToString() == "auto";

        public bool IsNotNormal => this != null && ToString() != "normal";

        public bool IsNormal => this != null && ToString() == "normal";

        public override string ToString()
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var term in Terms)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.AppendFormat("{0} ",
                        term.Separator.HasValue ? term.Separator.Value.ToString() : "");
                }
                sb.Append(term.ToString());
            }
            return sb.ToString();
        }

        public static implicit operator string(CssExpression e)
        {
            return e.ToString();
        }

        public static explicit operator double(CssExpression e)
        {
            return double.Parse(e.Terms.First().Value, CultureInfo.InvariantCulture);
        }

        public static explicit operator Emu(CssExpression e)
        {
            return Emu.PointsToEmus(double.Parse(e.Terms.First().Value, CultureInfo.InvariantCulture));
        }

        // will only be called on expression that is in terms of points
        public static explicit operator TPoint(CssExpression e)
        {
            return new TPoint(double.Parse(e.Terms.First().Value, CultureInfo.InvariantCulture));
        }

        // will only be called on expression that is in terms of points
        public static explicit operator Twip(CssExpression length)
        {
            if (length.Terms.Count == 1)
            {
                var term = length.Terms.First();
                if (term.Unit == CssUnit.PT && double.TryParse(term?.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ptValue))
                {
                    if (term.Sign == '-')
                    {
                        ptValue = -ptValue;
                    }

                    return new Twip((long)(ptValue * 20));
                }
            }
            return 0;
        }
    }

    public class CssFunction
    {
        public string Name { get; set; }

        public CssExpression Expression { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}(", Name);
            if (Expression != null)
            {
                var first = true;
                foreach (var t in Expression.Terms)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else if (!t.Value.EndsWith("="))
                    {
                        sb.Append(", ");
                    }

                    var quote = false;
                    if (t.Type == CssTermType.String && !t.Value.EndsWith("="))
                    {
                        quote = true;
                    }
                    if (quote)
                    {
                        sb.Append("'");
                    }
                    sb.Append(t.ToString());
                    if (quote)
                    {
                        sb.Append("'");
                    }
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
    }

    public interface IDeclarationContainer
    {
        IList<CssDeclaration> Declarations { get; set; }
    }

    public interface IRuleSetContainer
    {
        IList<CssRuleSet> RuleSets { get; set; }
    }

    public interface ISelectorContainer
    {
        IList<CssSelector> Selectors { get; set; }
    }

    public enum CssMedium
    {
        all,
        aural,
        braille,
        embossed,
        handheld,
        print,
        projection,
        screen,
        tty,
        tv
    }

    public class CssPropertyValue
    {
        public CssValueType Type { get; set; }

        public CssUnit Unit { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder(Value);
            if (Type == CssValueType.Unit)
            {
                sb.Append(Unit.ToString().ToLower());
            }
            sb.Append(" [");
            sb.Append(Type.ToString());
            sb.Append("]");
            return sb.ToString();
        }

        public bool IsColor
        {
            get
            {
                if ((Type == CssValueType.Hex
                    || Type == CssValueType.String && Value.StartsWith("#"))
                    && (Value.Length == 6 || Value.Length == 7 && Value.StartsWith("#")))
                {
                    var hex = true;
                    foreach (var c in Value)
                    {
                        if (!char.IsDigit(c)
                            && c != '#'
                            && c != 'a'
                            && c != 'A'
                            && c != 'b'
                            && c != 'B'
                            && c != 'c'
                            && c != 'C'
                            && c != 'd'
                            && c != 'D'
                            && c != 'e'
                            && c != 'E'
                            && c != 'f'
                            && c != 'F'
                        )
                        {
                            return false;
                        }
                    }
                    return hex;
                }
                else if (Type == CssValueType.String)
                {
                    var number = true;
                    foreach (var c in Value)
                    {
                        if (!char.IsDigit(c))
                        {
                            number = false;
                            break;
                        }
                    }
                    if (number) { return false; }

                    if (ColorParser.IsValidName(Value))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public SKColor ToColor()
        {
            var hex = "000000";
            if (Type == CssValueType.Hex)
            {
                if (Value.Length == 7 && Value.StartsWith("#"))
                {
                    hex = Value.Substring(1);
                }
                else if (Value.Length == 6)
                {
                    hex = Value;
                }
            }
            else
            {
                if (ColorParser.TryFromName(Value, out var c))
                {
                    return c;
                }
            }
            var r = ConvertFromHex(hex.Substring(0, 2));
            var g = ConvertFromHex(hex.Substring(2, 2));
            var b = ConvertFromHex(hex.Substring(4));
            return new SKColor(r, g, b);
        }

        private byte ConvertFromHex(string input)
        {
            int val;
            var result = 0;
            for (var i = 0; i < input.Length; i++)
            {
                var chunk = input.Substring(i, 1).ToUpper();
                val = chunk switch
                {
                    "A" => 10,
                    "B" => 11,
                    "C" => 12,
                    "D" => 13,
                    "E" => 14,
                    "F" => 15,
                    _ => int.Parse(chunk),
                };
                if (i == 0)
                {
                    result += val * 16;
                }
                else
                {
                    result += val;
                }
            }
            return BitConverter.GetBytes(result)[0];
        }
    }

    public class CssRuleSet : IDeclarationContainer
    {
        public IList<CssSelector> Selectors { get; set; } = new List<CssSelector>();

        public IList<CssDeclaration> Declarations { get; set; } = new List<CssDeclaration>();

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int indentLevel)
        {
            var start = new StringBuilder();
            for (var i = 0; i < indentLevel; i++)
            {
                start.Append("\t");
            }

            var sb = new StringBuilder();
            var first = true;
            foreach (var sel in Selectors)
            {
                if (first)
                {
                    first = false;
                    sb.Append(start);
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(sel.ToString());
            }
            sb.Append(" {" + Environment.NewLine);
            sb.Append(start);

            foreach (var dec in Declarations)
            {
                sb.AppendFormat("\t{0};" + Environment.NewLine + "{1}", dec.ToString(), start);
            }

            sb.Append("}");
            return sb.ToString();
        }
    }

    public class CssSelector
    {
        public List<CssSimpleSelector> SimpleSelectors { get; set; } = new List<CssSimpleSelector>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var ss in SimpleSelectors)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(" ");
                }
                sb.Append(ss.ToString());
            }
            return sb.ToString();
        }
    }

    public class CssSimpleSelector
    {
        private string m_cls;
        private CssSimpleSelector m_child;

        public CssCombinator? Combinator { get; set; } = null;

        public string? CombinatorString
        {
            get
            {
                if (Combinator.HasValue)
                {
                    return Combinator.ToString();
                }
                else
                {
                    return null;
                }
            }
            set => Combinator = (CssCombinator)Enum.Parse(typeof(CssCombinator), value);
        }

        public string ElementName { get; set; }

        public string ID { get; set; }

        public string Class
        {
            get => m_cls;
            set => m_cls = value;
        }

        public string Pseudo { get; set; }

        public CssAttribute Attribute { get; set; }

        public CssFunction Function { get; set; }

        public CssSimpleSelector Child
        {
            get => m_child;
            set => m_child = value;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Combinator.HasValue)
            {
                switch (Combinator.Value)
                {
                    case CssCombinator.PrecededImmediatelyBy:
                        sb.Append(" + ");
                        break;

                    case CssCombinator.ChildOf:
                        sb.Append(" > ");
                        break;

                    case CssCombinator.PrecededBy:
                        sb.Append(" ~ ");
                        break;
                }
            }
            if (ElementName != null)
            {
                sb.Append(ElementName);
            }
            if (ID != null)
            {
                sb.AppendFormat("#{0}", ID);
            }
            if (m_cls != null)
            {
                sb.AppendFormat(".{0}", m_cls);
            }
            if (Pseudo != null)
            {
                sb.AppendFormat(":{0}", Pseudo);
            }
            if (Attribute != null)
            {
                sb.Append(Attribute.ToString());
            }
            if (Function != null)
            {
                sb.Append(Function.ToString());
            }
            if (m_child != null)
            {
                if (m_child.ElementName != null)
                {
                    sb.Append(" ");
                }
                sb.Append(m_child.ToString());
            }
            return sb.ToString();
        }
    }

    public class CssTag
    {
        public CssTagType TagType { get; set; }

        public bool IsIDSelector => Id != null;

        public bool HasName => Name != null;

        public bool HasClass => Class != null;

        public bool HasPseudoClass => Pseudo != null;

        public string Name { get; set; }

        public string Class { get; set; }

        public string Pseudo { get; set; }

        public string Id { get; set; }

        public char ParentRelationship { get; set; } = '\0';

        public CssTag SubTag { get; set; }

        public List<string> Attributes { get; set; } = new List<string>();

        public override string ToString()
        {
            var sb = new StringBuilder(ToShortString());

            if (SubTag != null)
            {
                sb.Append(" ");
                sb.Append(SubTag.ToString());
            }
            return sb.ToString();
        }

        public string ToShortString()
        {
            var sb = new StringBuilder();
            if (ParentRelationship != '\0')
            {
                sb.AppendFormat("{0} ", ParentRelationship.ToString());
            }
            if (HasName)
            {
                sb.Append(Name);
            }
            foreach (var atr in Attributes)
            {
                sb.AppendFormat("[{0}]", atr);
            }
            if (HasClass)
            {
                sb.Append(".");
                sb.Append(Class);
            }
            if (IsIDSelector)
            {
                sb.Append("#");
                sb.Append(Id);
            }
            if (HasPseudoClass)
            {
                sb.Append(":");
                sb.Append(Pseudo);
            }
            return sb.ToString();
        }
    }

    [Flags]
    public enum CssTagType
    {
        Named = 1,
        Classed = 2,
        IDed = 4,
        Pseudoed = 8,
        Directive = 16
    }

    public class CssTerm
    {
        public char? Separator { get; set; }

        public string? SeparatorChar
        {
            get => Separator.HasValue ? Separator.Value.ToString() : null;
            set => Separator = !string.IsNullOrEmpty(value) ? value?[0] : '\0';
        }

        public char? Sign { get; set; }

        public string? SignChar
        {
            get => Sign?.ToString() ?? null;
            set => Sign = !string.IsNullOrEmpty(value) ? value![0] : '\0';
        }

        public CssTermType Type { get; set; }

        public string? Value { get; set; }

        public CssUnit? Unit { get; set; }

        public string? UnitString
        {
            get
            {
                if (Unit.HasValue)
                {
                    return Unit.ToString();
                }
                else
                {
                    return null;
                }
            }
            set => Unit = (CssUnit)Enum.Parse(typeof(CssUnit), value);
        }

        public CssFunction Function { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Type == CssTermType.Function)
            {
                sb.Append(Function.ToString());
            }
            else if (Type == CssTermType.Url)
            {
                sb.AppendFormat("url('{0}')", Value);
            }
            else if (Type == CssTermType.Unicode)
            {
                sb.AppendFormat("U\\{0}", Value.ToUpper());
            }
            else if (Type == CssTermType.Hex)
            {
                sb.Append(Value.ToUpper());
            }
            else
            {
                if (Sign.HasValue)
                {
                    sb.Append(Sign.Value);
                }
                sb.Append(Value);
                if (Unit.HasValue)
                {
                    if (Unit.Value == CssUnit.Percent)
                    {
                        sb.Append("%");
                    }
                    else
                    {
                        sb.Append(CssUnitOutput.ToString(Unit.Value));
                    }
                }
            }

            return sb.ToString();
        }

        public bool IsColor
        {
            get
            {
                if ((Type == CssTermType.Hex || Type == CssTermType.String && Value.StartsWith("#"))
                    && (Value.Length == 6 || Value.Length == 3 || (Value.Length == 7 || Value.Length == 4)
                    && Value.StartsWith("#")))
                {
                    var hex = true;
                    foreach (var c in Value)
                    {
                        if (!char.IsDigit(c)
                            && c != '#'
                            && c != 'a'
                            && c != 'A'
                            && c != 'b'
                            && c != 'B'
                            && c != 'c'
                            && c != 'C'
                            && c != 'd'
                            && c != 'D'
                            && c != 'e'
                            && c != 'E'
                            && c != 'f'
                            && c != 'F'
                        )
                        {
                            return false;
                        }
                    }
                    return hex;
                }
                else if (Type == CssTermType.String)
                {
                    var number = true;
                    foreach (var c in Value)
                    {
                        if (!char.IsDigit(c))
                        {
                            number = false;
                            break;
                        }
                    }
                    if (number)
                    {
                        return false;
                    }

                    if (ColorParser.IsValidName(Value))
                    {
                        return true;
                    }
                }
                else if (Type == CssTermType.Function && ((Function.Name.ToLower().Equals("hsl") || Function.Name.ToLower().Equals("rgb")) && Function.Expression.Terms.Count == 3 ||
                        (Function.Name.ToLower().Equals("hsla") || Function.Name.ToLower().Equals("rgba")) && Function.Expression.Terms.Count == 4))
                {
                    for (var i = 0; i < Function.Expression.Terms.Count; i++)
                    {
                        if (Function.Expression.Terms[i].Type != CssTermType.Number)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
        }

        private byte GetRGBValue(CssTerm t)
        {
            try
            {
                if (t.Unit.HasValue && t.Unit.Value == CssUnit.Percent)
                {
                    return BitConverter.GetBytes((int)(255f * float.Parse(t.Value) / 100f))[0];
                }
                return BitConverter.GetBytes(int.Parse(t.Value))[0];
            }
            catch { }
            return 0;
        }

        private int GetHueValue(CssTerm t)
        {
            try
            {
                return (int)(float.Parse(t.Value) * 255f / 360f);
            }
            catch { }
            return 0;
        }

        public SKColor ToColor()
        {
            var hex = "000000";
            if (Type == CssTermType.Hex)
            {
                if ((Value.Length == 7 || Value.Length == 4) && Value.StartsWith("#"))
                {
                    hex = Value.Substring(1);
                }
                else if (Value.Length == 6 || Value.Length == 3)
                {
                    hex = Value;
                }
            }
            else if (Type == CssTermType.Function)
            {
                if (Function.Name.ToLower().Equals("rgb") && Function.Expression.Terms.Count == 3
                    || Function.Name.ToLower().Equals("rgba") && Function.Expression.Terms.Count == 4
                    )
                {
                    byte fr = 0, fg = 0, fb = 0;
                    for (var i = 0; i < Function.Expression.Terms.Count; i++)
                    {
                        if (Function.Expression.Terms[i].Type != CssTermType.Number)
                        {
                            return SKColors.Black;
                        }
                        switch (i)
                        {
                            case 0:
                                fr = GetRGBValue(Function.Expression.Terms[i]);
                                break;

                            case 1:
                                fg = GetRGBValue(Function.Expression.Terms[i]);
                                break;

                            case 2:
                                fb = GetRGBValue(Function.Expression.Terms[i]);
                                break;
                        }
                    }
                    return new SKColor(fr, fg, fb);
                }
                else if (Function.Name.ToLower().Equals("hsl") && Function.Expression.Terms.Count == 3
                  || Function.Name.Equals("hsla") && Function.Expression.Terms.Count == 4
                  )
                {
                    int h = 0, s = 0, v = 0;
                    for (var i = 0; i < Function.Expression.Terms.Count; i++)
                    {
                        if (Function.Expression.Terms[i].Type != CssTermType.Number) { return SKColors.Black; }
                        switch (i)
                        {
                            case 0:
                                h = GetHueValue(Function.Expression.Terms[i]);
                                break;

                            case 1:
                                s = GetRGBValue(Function.Expression.Terms[i]);
                                break;

                            case 2:
                                v = GetRGBValue(Function.Expression.Terms[i]);
                                break;
                        }
                    }
                    var hsv = new HueSatVal(h, s, v);
                    return hsv.Color;
                }
            }
            else
            {
                if (ColorParser.TryFromName(Value, out var c))
                {
                    return c;
                }
            }
            if (hex.Length == 3)
            {
                var temp = "";
                foreach (var c in hex)
                {
                    temp += c.ToString() + c.ToString();
                }
                hex = temp;
            }
            var r = ConvertFromHex(hex.Substring(0, 2));
            var g = ConvertFromHex(hex.Substring(2, 2));
            var b = ConvertFromHex(hex.Substring(4));
            return new SKColor(r, g, b);
        }

        private byte ConvertFromHex(string input)
        {
            int val;
            var result = 0;
            for (var i = 0; i < input.Length; i++)
            {
                var chunk = input.Substring(i, 1).ToUpper();
                val = chunk switch
                {
                    "A" => 10,
                    "B" => 11,
                    "C" => 12,
                    "D" => 13,
                    "E" => 14,
                    "F" => 15,
                    _ => int.Parse(chunk),
                };
                if (i == 0)
                {
                    result += val * 16;
                }
                else
                {
                    result += val;
                }
            }
            return BitConverter.GetBytes(result)[0];
        }
    }

    public enum CssTermType
    {
        Number,
        Function,
        String,
        Url,
        Unicode,
        Hex
    }

    public enum CssUnit
    {
        None,
        Percent,
        EM,
        EX,
        PX,
        GD,
        REM,
        VW,
        VH,
        VM,
        CH,
        MM,
        CM,
        IN,
        PT,
        PC,
        DEG,
        GRAD,
        RAD,
        TURN,
        MS,
        S,
        Hz,
        kHz,
    }

    public static class CssUnitOutput
    {
        public static string ToString(CssUnit u)
        {
            if (u == CssUnit.Percent)
            {
                return "%";
            }
            else if (u == CssUnit.Hz || u == CssUnit.kHz)
            {
                return u.ToString();
            }
            else if (u == CssUnit.None)
            {
                return "";
            }
            return u.ToString().ToLower();
        }
    }

    public enum CssValueType
    {
        String,
        Hex,
        Unit,
        Percent,
        Url,
        Function
    }

    public class CssParser
    {
        private CssDocument m_doc;

        public CssDocument ParseText(string content)
        {
            var mem = new MemoryStream();
            var bytes = Encoding.ASCII.GetBytes(content);
            mem.Write(bytes, 0, bytes.Length);
            try
            {
                return ParseStream(mem);
            }
            catch (OpenXmlPowerToolsException e)
            {
                var msg = e.Message + ".  CSS => " + content;
                throw new OpenXmlPowerToolsException(msg);
            }
        }

        // following method should be private, as it does not properly re-throw OpenXmlPowerToolsException
        private CssDocument ParseStream(Stream stream)
        {
            var scanner = new Scanner(stream);
            var parser = new Parser(scanner);
            parser.Parse();
            m_doc = parser.CssDoc;
            return m_doc;
        }

        public CssDocument CSSDocument => m_doc;

        public List<string> Errors { get; } = new List<string>();
    }

    // Hue Sat and Val values from 0 - 255.
    internal struct HueSatVal
    {
        public HueSatVal(int h, int s, int v)
        {
            Hue = h;
            Saturation = s;
            Value = v;
        }

        public HueSatVal(SKColor color)
        {
            Hue = 0;
            Saturation = 0;
            Value = 0;
            ConvertFromRGB(color);
        }

        public int Hue { get; set; }

        public int Saturation { get; set; }

        public int Value { get; set; }

        public SKColor Color
        {
            get => ConvertToRGB();
            set => ConvertFromRGB(value);
        }

        private void ConvertFromRGB(SKColor color)
        {
            double min; double max; double delta;
            var r = CalcRedConponent(color);
            var g = CalcGreenConponent(color);
            var b = CalcBlueConponent(color);
            double h; double s; double v;

            min = Math.Min(Math.Min(r, g), b);
            max = Math.Max(Math.Max(r, g), b);
            v = max;
            delta = max - min;
            if (max == 0 || delta == 0)
            {
                s = 0;
                h = 0;
            }
            else
            {
                s = delta / max;
                if (r == max)
                {
                    h = 60D * ((g - b) / delta) % 360.0d;
                }
                else if (g == max)
                {
                    h = 60D * ((b - r) / delta) + 120.0d;
                }
                else
                {
                    h = 60D * ((r - g) / delta) + 240.0d;
                }
            }
            if (h < 0)
            {
                h += 360.0d;
            }

            Hue = (int)(h / 360.0d * 255.0d);
            Saturation = (int)(s * 255.0d);
            Value = (int)(v * 255.0d);
        }

        private double CalcRedConponent(SKColor color)
        {
            return color.Red;
        }

        private double CalcGreenConponent(SKColor color)
        {
            return color.Green;
        }

        private double CalcBlueConponent(SKColor color)
        {
            return color.Blue;
        }

        private SKColor ConvertToRGB()
        {
            double h;
            double s;
            double v;
            double r = 0;
            double g = 0;
            double b = 0;

            h = Hue / 255.0d * 360.0d % 360.0d;
            s = Saturation / 255.0d;
            v = Value / 255.0d;

            if (s == 0)
            {
                r = v;
                g = v;
                b = v;
            }
            else
            {
                double p;
                double q;
                double t;

                double fractionalPart;
                int sectorNumber;
                double sectorPos;

                sectorPos = h / 60.0d;
                sectorNumber = (int)Math.Floor(sectorPos);

                fractionalPart = sectorPos - sectorNumber;

                p = v * (1.0d - s);
                q = v * (1.0d - s * fractionalPart);
                t = v * (1.0d - s * (1.0d - fractionalPart));

                switch (sectorNumber)
                {
                    case 0:
                        r = v;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = v;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = v;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = v;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = v;
                        break;

                    case 5:
                        r = v;
                        g = p;
                        b = q;
                        break;
                }
            }
            return new SKColor((byte)(r * 255.0d), (byte)(g * 255.0d), (byte)(b * 255.0d));
        }

        public static bool operator !=(HueSatVal left, HueSatVal right)
        {
            return !(left == right);
        }

        public static bool operator ==(HueSatVal left, HueSatVal right)
        {
            return left.Hue == right.Hue && left.Value == right.Value && left.Saturation == right.Saturation;
        }

        public override bool Equals(object obj)
        {
            return this == (HueSatVal)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class Parser
    {
        public const int c_EOF = 0;
        public const int c_ident = 1;
        public const int c_newline = 2;
        public const int c_digit = 3;
        public const int c_whitespace = 4;
        public const int c_maxT = 49;

        private const bool T = true;
        private const bool x = false;
        private const int minErrDist = 2;

        public Scanner Scanner { get; set; }
        public Errors Errors { get; set; }

        public CssToken LastRecognizedToken { get; set; }
        public CssToken LookaheadToken { get; set; }
        private int errDist = minErrDist;

        public CssDocument CssDoc { get; set; }

        private bool IsInHex(string value)
        {
            if (value.Length == 7)
            {
                return false;
            }
            if (value.Length + LookaheadToken.TokenValue.Length > 7)
            {
                return false;
            }
            var hexes = new List<string>
            {
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "a", "b", "c", "d", "e", "f"
            };
            foreach (var c in LookaheadToken.TokenValue)
            {
                if (!hexes.Contains(c.ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsUnitOfLength()
        {
            if (LookaheadToken.TokenKind != 1)
            {
                return false;
            }
            var units = new List<string>(
                new string[]
                {
                    "em", "ex", "px", "gd", "rem", "vw", "vh", "vm", "ch", "mm", "cm", "in", "pt", "pc", "deg", "grad", "rad", "turn", "ms", "s", "hz", "khz"
                });
            return units.Contains(LookaheadToken.TokenValue.ToLower());
        }

        public Parser(Scanner scanner)
        {
            Scanner = scanner;
            Errors = new Errors();
        }

        private void SyntaxErr(int n)
        {
            if (errDist >= minErrDist)
            {
                Errors.SyntaxError(LookaheadToken.TokenLine, LookaheadToken.TokenColumn, n);
            }

            errDist = 0;
        }

        public void SemanticErr(string msg)
        {
            if (errDist >= minErrDist)
            {
                Errors.SemanticError(LastRecognizedToken.TokenLine, LastRecognizedToken.TokenColumn, msg);
            }

            errDist = 0;
        }

        private void Get()
        {
            for (; ; )
            {
                LastRecognizedToken = LookaheadToken;
                LookaheadToken = Scanner.Scan();
                if (LookaheadToken.TokenKind <= c_maxT)
                {
                    ++errDist;
                    break;
                }

                LookaheadToken = LastRecognizedToken;
            }
        }

        private void Expect(int n)
        {
            if (LookaheadToken.TokenKind == n)
            {
                Get();
            }
            else
            {
                SyntaxErr(n);
            }
        }

        private bool StartOf(int s)
        {
            return set[s, LookaheadToken.TokenKind];
        }

        private void Css3()
        {
            CssDoc = new CssDocument();
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            while (LookaheadToken.TokenKind == 5 || LookaheadToken.TokenKind == 6)
            {
                if (LookaheadToken.TokenKind == 5)
                {
                    Get();
                }
                else
                {
                    Get();
                }
            }
            while (StartOf(1))
            {
                if (StartOf(2))
                {
                    RuleSet(out var rset);
                    CssDoc.RuleSets.Add(rset);
                }
                else
                {
                    Directive(out var dir);
                    CssDoc.Directives.Add(dir);
                }
                while (LookaheadToken.TokenKind == 5 || LookaheadToken.TokenKind == 6)
                {
                    if (LookaheadToken.TokenKind == 5)
                    {
                        Get();
                    }
                    else
                    {
                        Get();
                    }
                }
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
        }

        private void RuleSet(out CssRuleSet rset)
        {
            rset = new CssRuleSet();
            Selector(out var sel);
            rset.Selectors.Add(sel);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            while (LookaheadToken.TokenKind == 25)
            {
                Get();
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                Selector(out sel);
                rset.Selectors.Add(sel);
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
            Expect(26);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (StartOf(3))
            {
                Declaration(out var dec);
                rset.Declarations.Add(dec);
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                while (LookaheadToken.TokenKind == 27)
                {
                    Get();
                    while (LookaheadToken.TokenKind == 4)
                    {
                        Get();
                    }
                    if (LookaheadToken.TokenValue.Equals("}"))
                    {
                        Get();
                        return;
                    }

                    Declaration(out dec);
                    rset.Declarations.Add(dec);
                    while (LookaheadToken.TokenKind == 4)
                    {
                        Get();
                    }
                }
                if (LookaheadToken.TokenKind == 27)
                {
                    Get();
                    while (LookaheadToken.TokenKind == 4)
                    {
                        Get();
                    }
                }
            }
            Expect(28);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
        }

        private void Directive(out CssDirective dir)
        {
            dir = new CssDirective();
            Expect(23);
            dir.Name = "@";
            if (LookaheadToken.TokenKind == 24)
            {
                Get();
                dir.Name += "-";
            }
            Identity(out var ident);
            dir.Name += ident;
            dir.Type = dir.Name.ToLower() switch
            {
                "@media" => CssDirectiveType.Media,
                "@import" => CssDirectiveType.Import,
                "@charset" => CssDirectiveType.Charset,
                "@page" => CssDirectiveType.Page,
                "@font-face" => CssDirectiveType.FontFace,
                "@namespace" => CssDirectiveType.Namespace,
                _ => CssDirectiveType.Other,
            };
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (StartOf(4))
            {
                if (StartOf(5))
                {
                    Medium(out var m);
                    dir.Mediums.Add(m);
                    while (LookaheadToken.TokenKind == 4)
                    {
                        Get();
                    }
                    while (LookaheadToken.TokenKind == 25)
                    {
                        Get();
                        while (LookaheadToken.TokenKind == 4)
                        {
                            Get();
                        }
                        Medium(out m);
                        dir.Mediums.Add(m);
                        while (LookaheadToken.TokenKind == 4)
                        {
                            Get();
                        }
                    }
                }
                else
                {
                    Exprsn(out var exp);
                    dir.Expression = exp;
                    while (LookaheadToken.TokenKind == 4)
                    {
                        Get();
                    }
                }
            }
            if (LookaheadToken.TokenKind == 26)
            {
                Get();
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                if (StartOf(6))
                {
                    while (StartOf(1))
                    {
                        if (dir.Type == CssDirectiveType.Page || dir.Type == CssDirectiveType.FontFace)
                        {
                            Declaration(out var dec);
                            dir.Declarations.Add(dec);
                            while (LookaheadToken.TokenKind == 4)
                            {
                                Get();
                            }
                            while (LookaheadToken.TokenKind == 27)
                            {
                                Get();
                                while (LookaheadToken.TokenKind == 4)
                                {
                                    Get();
                                }
                                if (LookaheadToken.TokenValue.Equals("}"))
                                {
                                    Get();
                                    return;
                                }
                                Declaration(out dec);
                                dir.Declarations.Add(dec);
                                while (LookaheadToken.TokenKind == 4)
                                {
                                    Get();
                                }
                            }
                            if (LookaheadToken.TokenKind == 27)
                            {
                                Get();
                                while (LookaheadToken.TokenKind == 4)
                                {
                                    Get();
                                }
                            }
                        }
                        else if (StartOf(2))
                        {
                            RuleSet(out var rset);
                            dir.RuleSets.Add(rset);
                            while (LookaheadToken.TokenKind == 4)
                            {
                                Get();
                            }
                        }
                        else
                        {
                            Directive(out var dr);
                            dir.Directives.Add(dr);
                            while (LookaheadToken.TokenKind == 4)
                            {
                                Get();
                            }
                        }
                    }
                }
                Expect(28);
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
            else if (LookaheadToken.TokenKind == 27)
            {
                Get();
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
            else
            {
                SyntaxErr(50);
            }
        }

        private void QuotedString(out string qs)
        {
            qs = "";
            if (LookaheadToken.TokenKind == 7)
            {
                Get();
                while (StartOf(7))
                {
                    Get();
                    qs += LastRecognizedToken.TokenValue;
                    if (LookaheadToken.TokenValue.Equals("'") && !LastRecognizedToken.TokenValue.Equals("\\"))
                    {
                        break;
                    }
                }
                Expect(7);
            }
            else if (LookaheadToken.TokenKind == 8)
            {
                Get();
                while (StartOf(8))
                {
                    Get();
                    qs += LastRecognizedToken.TokenValue;
                    if (LookaheadToken.TokenValue.Equals("\"") && !LastRecognizedToken.TokenValue.Equals("\\"))
                    {
                        break;
                    }
                }
                Expect(8);
            }
            else
            {
                SyntaxErr(51);
            }
        }

        private void URI(out string url)
        {
            url = "";
            Expect(9);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (LookaheadToken.TokenKind == 10)
            {
                Get();
            }
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (LookaheadToken.TokenKind == 7 || LookaheadToken.TokenKind == 8)
            {
                QuotedString(out url);
            }
            else if (StartOf(9))
            {
                while (StartOf(10))
                {
                    Get();
                    url += LastRecognizedToken.TokenValue;
                    if (LookaheadToken.TokenValue.Equals(")"))
                    {
                        break;
                    }
                }
            }
            else
            {
                SyntaxErr(52);
            }

            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (LookaheadToken.TokenKind == 11)
            {
                Get();
            }
        }

        private void Medium(out CssMedium m)
        {
            m = CssMedium.all;
            switch (LookaheadToken.TokenKind)
            {
                case 12:
                    {
                        Get();
                        m = CssMedium.all;
                        break;
                    }
                case 13:
                    {
                        Get();
                        m = CssMedium.aural;
                        break;
                    }
                case 14:
                    {
                        Get();
                        m = CssMedium.braille;
                        break;
                    }
                case 15:
                    {
                        Get();
                        m = CssMedium.embossed;
                        break;
                    }
                case 16:
                    {
                        Get();
                        m = CssMedium.handheld;
                        break;
                    }
                case 17:
                    {
                        Get();
                        m = CssMedium.print;
                        break;
                    }
                case 18:
                    {
                        Get();
                        m = CssMedium.projection;
                        break;
                    }
                case 19:
                    {
                        Get();
                        m = CssMedium.screen;
                        break;
                    }
                case 20:
                    {
                        Get();
                        m = CssMedium.tty;
                        break;
                    }
                case 21:
                    {
                        Get();
                        m = CssMedium.tv;
                        break;
                    }
                default: SyntaxErr(53); break;
            }
        }

        private void Identity(out string ident)
        {
            ident = "";
            switch (LookaheadToken.TokenKind)
            {
                case 1:
                    {
                        Get();
                        break;
                    }
                case 22:
                    {
                        Get();
                        break;
                    }
                case 9:
                    {
                        Get();
                        break;
                    }
                case 12:
                    {
                        Get();
                        break;
                    }
                case 13:
                    {
                        Get();
                        break;
                    }
                case 14:
                    {
                        Get();
                        break;
                    }
                case 15:
                    {
                        Get();
                        break;
                    }
                case 16:
                    {
                        Get();
                        break;
                    }
                case 17:
                    {
                        Get();
                        break;
                    }
                case 18:
                    {
                        Get();
                        break;
                    }
                case 19:
                    {
                        Get();
                        break;
                    }
                case 20:
                    {
                        Get();
                        break;
                    }
                case 21:
                    {
                        Get();
                        break;
                    }
                default: SyntaxErr(54); break;
            }
            ident += LastRecognizedToken.TokenValue;
        }

        private void Exprsn(out CssExpression exp)
        {
            exp = new CssExpression();
            char? sep = null;

            Term(out var trm);
            exp.Terms.Add(trm);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            while (StartOf(11))
            {
                if (LookaheadToken.TokenKind == 25 || LookaheadToken.TokenKind == 46)
                {
                    if (LookaheadToken.TokenKind == 46)
                    {
                        Get();
                        sep = '/';
                    }
                    else
                    {
                        Get();
                        sep = ',';
                    }
                    while (LookaheadToken.TokenKind == 4)
                    {
                        Get();
                    }
                }
                Term(out trm);
                if (sep.HasValue)
                {
                    trm.Separator = sep.Value;
                }
                exp.Terms.Add(trm);
                sep = null;

                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
        }

        private void Declaration(out CssDeclaration dec)
        {
            dec = new CssDeclaration();

            if (LookaheadToken.TokenKind == 24)
            {
                Get();
                dec.Name += "-";
            }
            Identity(out var ident);
            dec.Name += ident;
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            Expect(43);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            Exprsn(out var exp);
            dec.Expression = exp;
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (LookaheadToken.TokenKind == 44)
            {
                Get();
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                Expect(45);
                dec.Important = true;
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
        }

        private void Selector(out CssSelector sel)
        {
            sel = new CssSelector();
            CssCombinator? cb = null;

            SimpleSelector(out var ss);
            sel.SimpleSelectors.Add(ss);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            while (StartOf(12))
            {
                if (LookaheadToken.TokenKind == 29 || LookaheadToken.TokenKind == 30 || LookaheadToken.TokenKind == 31)
                {
                    if (LookaheadToken.TokenKind == 29)
                    {
                        Get();
                        cb = CssCombinator.PrecededImmediatelyBy;
                    }
                    else if (LookaheadToken.TokenKind == 30)
                    {
                        Get();
                        cb = CssCombinator.ChildOf;
                    }
                    else
                    {
                        Get();
                        cb = CssCombinator.PrecededBy;
                    }
                }
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                SimpleSelector(out ss);
                if (cb.HasValue)
                {
                    ss.Combinator = cb.Value;
                }
                sel.SimpleSelectors.Add(ss);

                cb = null;
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
        }

        private void SimpleSelector(out CssSimpleSelector ss)
        {
            ss = new CssSimpleSelector
            {
                ElementName = ""
            };
            var parent = ss;
            string psd;
            CssAttribute atb;
            string ident;
            if (StartOf(3))
            {
                if (LookaheadToken.TokenKind == 24)
                {
                    Get();
                    ss.ElementName += "-";
                }
                Identity(out ident);
                ss.ElementName += ident;
            }
            else if (LookaheadToken.TokenKind == 32)
            {
                Get();
                ss.ElementName = "*";
            }
            else if (StartOf(13))
            {
                if (LookaheadToken.TokenKind == 33)
                {
                    Get();
                    if (LookaheadToken.TokenKind == 24)
                    {
                        Get();
                        ss.ID = "-";
                    }
                    Identity(out ident);
                    if (ss.ID == null)
                    {
                        ss.ID = ident;
                    }
                    else
                    {
                        ss.ID += ident;
                    }
                }
                else if (LookaheadToken.TokenKind == 34)
                {
                    Get();
                    ss.Class = "";
                    if (LookaheadToken.TokenKind == 24)
                    {
                        Get();
                        ss.Class += "-";
                    }
                    Identity(out ident);
                    ss.Class += ident;
                }
                else if (LookaheadToken.TokenKind == 35)
                {
                    Attrib(out atb);
                    ss.Attribute = atb;
                }
                else
                {
                    Pseudo(out psd);
                    ss.Pseudo = psd;
                }
            }
            else
            {
                SyntaxErr(55);
            }

            while (StartOf(13))
            {
                var child = new CssSimpleSelector();
                if (LookaheadToken.TokenKind == 33)
                {
                    Get();
                    if (LookaheadToken.TokenKind == 24)
                    {
                        Get();
                        child.ID = "-";
                    }
                    Identity(out ident);
                    if (child.ID == null)
                    {
                        child.ID = ident;
                    }
                    else
                    {
                        child.ID += "-";
                    }
                }
                else if (LookaheadToken.TokenKind == 34)
                {
                    Get();
                    child.Class = "";
                    if (LookaheadToken.TokenKind == 24)
                    {
                        Get();
                        child.Class += "-";
                    }
                    Identity(out ident);
                    child.Class += ident;
                }
                else if (LookaheadToken.TokenKind == 35)
                {
                    Attrib(out atb);
                    child.Attribute = atb;
                }
                else
                {
                    Pseudo(out psd);
                    child.Pseudo = psd;
                }
                parent.Child = child;
                parent = child;
            }
        }

        private void Attrib(out CssAttribute atb)
        {
            atb = new CssAttribute
            {
                Value = ""
            };
            Expect(35);
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            Identity(out var ident);
            atb.Operand = ident;
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (StartOf(14))
            {
                switch (LookaheadToken.TokenKind)
                {
                    case 36:
                        {
                            Get();
                            atb.Operator = CssAttributeOperator.Equals;
                            break;
                        }
                    case 37:
                        {
                            Get();
                            atb.Operator = CssAttributeOperator.InList;
                            break;
                        }
                    case 38:
                        {
                            Get();
                            atb.Operator = CssAttributeOperator.Hyphenated;
                            break;
                        }
                    case 39:
                        {
                            Get();
                            atb.Operator = CssAttributeOperator.EndsWith;
                            break;
                        }
                    case 40:
                        {
                            Get();
                            atb.Operator = CssAttributeOperator.BeginsWith;
                            break;
                        }
                    case 41:
                        {
                            Get();
                            atb.Operator = CssAttributeOperator.Contains;
                            break;
                        }
                }
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                if (StartOf(3))
                {
                    if (LookaheadToken.TokenKind == 24)
                    {
                        Get();
                        atb.Value += "-";
                    }
                    Identity(out ident);
                    atb.Value += ident;
                }
                else if (LookaheadToken.TokenKind == 7 || LookaheadToken.TokenKind == 8)
                {
                    QuotedString(out var quote);
                    atb.Value = quote;
                }
                else
                {
                    SyntaxErr(56);
                }

                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
            }
            Expect(42);
        }

        private void Pseudo(out string pseudo)
        {
            pseudo = "";
            Expect(43);
            if (LookaheadToken.TokenKind == 43)
            {
                Get();
            }
            while (LookaheadToken.TokenKind == 4)
            {
                Get();
            }
            if (LookaheadToken.TokenKind == 24)
            {
                Get();
                pseudo += "-";
            }
            Identity(out var ident);
            pseudo += ident;
            if (LookaheadToken.TokenKind == 10)
            {
                Get();
                pseudo += LastRecognizedToken.TokenValue;
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                Exprsn(out var exp);
                pseudo += exp.ToString();
                while (LookaheadToken.TokenKind == 4)
                {
                    Get();
                }
                Expect(11);
                pseudo += LastRecognizedToken.TokenValue;
            }
        }

        private void Term(out CssTerm trm)
        {
            trm = new CssTerm();
            var val = "";
            string ident;
            if (LookaheadToken.TokenKind == 7 || LookaheadToken.TokenKind == 8)
            {
                QuotedString(out val);
                trm.Value = val; trm.Type = CssTermType.String;
            }
            else if (LookaheadToken.TokenKind == 9)
            {
                URI(out val);
                trm.Value = val;
                trm.Type = CssTermType.Url;
            }
            else if (LookaheadToken.TokenKind == 47)
            {
                Get();
                Identity(out ident);
                trm.Value = "U\\" + ident;
                trm.Type = CssTermType.Unicode;
            }
            else if (LookaheadToken.TokenKind == 33)
            {
                HexValue(out val);
                trm.Value = val;
                trm.Type = CssTermType.Hex;
            }
            else if (StartOf(15))
            {
                var minus = false;
                if (LookaheadToken.TokenKind == 24)
                {
                    Get();
                    minus = true;
                }
                if (StartOf(16))
                {
                    Identity(out ident);
                    trm.Value = ident;
                    trm.Type = CssTermType.String;
                    if (minus)
                    {
                        trm.Value = "-" + trm.Value;
                    }
                    if (StartOf(17))
                    {
                        while (LookaheadToken.TokenKind == 34 || LookaheadToken.TokenKind == 36 || LookaheadToken.TokenKind == 43)
                        {
                            if (LookaheadToken.TokenKind == 43)
                            {
                                Get();
                                trm.Value += LastRecognizedToken.TokenValue;
                                if (StartOf(18))
                                {
                                    if (LookaheadToken.TokenKind == 43)
                                    {
                                        Get();
                                        trm.Value += LastRecognizedToken.TokenValue;
                                    }
                                    if (LookaheadToken.TokenKind == 24)
                                    {
                                        Get();
                                        trm.Value += LastRecognizedToken.TokenValue;
                                    }
                                    Identity(out ident);
                                    trm.Value += ident;
                                }
                                else if (LookaheadToken.TokenKind == 33)
                                {
                                    HexValue(out val);
                                    trm.Value += val;
                                }
                                else if (StartOf(19))
                                {
                                    while (LookaheadToken.TokenKind == 3)
                                    {
                                        Get();
                                        trm.Value += LastRecognizedToken.TokenValue;
                                    }
                                    if (LookaheadToken.TokenKind == 34)
                                    {
                                        Get();
                                        trm.Value += ".";
                                        while (LookaheadToken.TokenKind == 3)
                                        {
                                            Get();
                                            trm.Value += LastRecognizedToken.TokenValue;
                                        }
                                    }
                                }
                                else
                                {
                                    SyntaxErr(57);
                                }
                            }
                            else if (LookaheadToken.TokenKind == 34)
                            {
                                Get();
                                trm.Value += LastRecognizedToken.TokenValue;
                                if (LookaheadToken.TokenKind == 24)
                                {
                                    Get();
                                    trm.Value += LastRecognizedToken.TokenValue;
                                }
                                Identity(out ident);
                                trm.Value += ident;
                            }
                            else
                            {
                                Get();
                                trm.Value += LastRecognizedToken.TokenValue;
                                if (LookaheadToken.TokenKind == 24)
                                {
                                    Get();
                                    trm.Value += LastRecognizedToken.TokenValue;
                                }
                                if (StartOf(16))
                                {
                                    Identity(out ident);
                                    trm.Value += ident;
                                }
                                else if (StartOf(19))
                                {
                                    while (LookaheadToken.TokenKind == 3)
                                    {
                                        Get();
                                        trm.Value += LastRecognizedToken.TokenValue;
                                    }
                                }
                                else
                                {
                                    SyntaxErr(58);
                                }
                            }
                        }
                    }
                    if (LookaheadToken.TokenKind == 10)
                    {
                        Get();
                        while (LookaheadToken.TokenKind == 4)
                        {
                            Get();
                        }
                        Exprsn(out var exp);
                        var func = new CssFunction
                        {
                            Name = trm.Value,
                            Expression = exp
                        };
                        trm.Value = null;
                        trm.Function = func;
                        trm.Type = CssTermType.Function;

                        while (LookaheadToken.TokenKind == 4)
                        {
                            Get();
                        }
                        Expect(11);
                    }
                }
                else if (StartOf(15))
                {
                    if (LookaheadToken.TokenKind == 29)
                    {
                        Get();
                        trm.Sign = '+';
                    }
                    if (minus) { trm.Sign = '-'; }
                    while (LookaheadToken.TokenKind == 3)
                    {
                        Get();
                        val += LastRecognizedToken.TokenValue;
                    }
                    if (LookaheadToken.TokenKind == 34)
                    {
                        Get();
                        val += LastRecognizedToken.TokenValue;
                        while (LookaheadToken.TokenKind == 3)
                        {
                            Get();
                            val += LastRecognizedToken.TokenValue;
                        }
                    }
                    if (StartOf(20))
                    {
                        if (LookaheadToken.TokenValue.ToLower().Equals("n"))
                        {
                            Expect(22);
                            val += LastRecognizedToken.TokenValue;
                            if (LookaheadToken.TokenKind == 24 || LookaheadToken.TokenKind == 29)
                            {
                                Get();
                                val += LastRecognizedToken.TokenValue;

                                Expect(3);
                                val += LastRecognizedToken.TokenValue;
                                while (LookaheadToken.TokenKind == 3)
                                {
                                    Get();
                                    val += LastRecognizedToken.TokenValue;
                                }
                            }
                        }
                        else if (LookaheadToken.TokenKind == 48)
                        {
                            Get();
                            trm.Unit = CssUnit.Percent;
                        }
                        else
                        {
                            if (IsUnitOfLength())
                            {
                                Identity(out ident);
                                try
                                {
                                    trm.Unit = (CssUnit)Enum.Parse(typeof(CssUnit), ident, true);
                                }
                                catch
                                {
                                    Errors.SemanticError(LastRecognizedToken.TokenLine, LastRecognizedToken.TokenColumn, string.Format("Unrecognized unit '{0}'", ident));
                                }
                            }
                        }
                    }
                    trm.Value = val; trm.Type = CssTermType.Number;
                }
                else
                {
                    SyntaxErr(59);
                }
            }
            else
            {
                SyntaxErr(60);
            }
        }

        private void HexValue(out string val)
        {
            val = "";
            var found = false;

            Expect(33);
            val += LastRecognizedToken.TokenValue;
            if (StartOf(19))
            {
                while (LookaheadToken.TokenKind == 3)
                {
                    Get();
                    val += LastRecognizedToken.TokenValue;
                }
            }
            else if (IsInHex(val))
            {
                Expect(1);
                val += LastRecognizedToken.TokenValue; found = true;
            }
            else
            {
                SyntaxErr(61);
            }

            if (!found && IsInHex(val))
            {
                Expect(1);
                val += LastRecognizedToken.TokenValue;
            }
        }

        public void Parse()
        {
            LookaheadToken = new CssToken
            {
                TokenValue = ""
            };
            Get();
            Css3();
            Expect(0);
        }

        private static readonly bool[,] set = {
            {T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
            {x,T,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x},
            {x,T,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, T,x,x,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x},
            {x,T,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
            {x,T,x,T, T,x,x,T, T,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, T,T,T,T, x,T,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,T,T, T,x,x},
            {x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
            {x,T,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x,x, T,T,T,T, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x},
            {x,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x},
            {x,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x},
            {x,T,T,T, T,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x},
            {x,T,T,T, x,T,T,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x},
            {x,T,x,T, T,x,x,T, T,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, T,T,x,x, x,T,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,T,T, T,x,x},
            {x,T,x,x, T,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, T,x,x,x, x,T,T,T, T,T,T,T, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x},
            {x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x},
            {x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x},
            {x,T,x,T, T,x,x,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,x, T,T,T,T, x,x,x,x, x,x,x,T, T,x,T,T, T,x,x},
            {x,T,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
            {x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,T, x,x,x,x, x,x,x},
            {x,T,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x},
            {x,T,x,T, T,x,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,x, T,T,T,T, T,x,x,x, x,x,x,T, T,x,T,T, T,x,x},
            {x,T,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x}
        };
    }

    public class Errors
    {
        public int NumberOfErrorsDetected { get; set; } = 0;
        public string ErrMsgFormat { get; set; } = "CssParser error: line {0} col {1}: {2}";

        public virtual void SyntaxError(int line, int col, int n)
        {
            var s = n switch
            {
                0 => "EOF expected",
                1 => "identifier expected",
                2 => "newline expected",
                3 => "digit expected",
                4 => "whitespace expected",
                5 => "\"<!--\" expected",
                6 => "\"-->\" expected",
                7 => "\"\'\" expected",
                8 => "\"\"\" expected",
                9 => "\"url\" expected",
                10 => "\"(\" expected",
                11 => "\")\" expected",
                12 => "\"all\" expected",
                13 => "\"aural\" expected",
                14 => "\"braille\" expected",
                15 => "\"embossed\" expected",
                16 => "\"handheld\" expected",
                17 => "\"print\" expected",
                18 => "\"projection\" expected",
                19 => "\"screen\" expected",
                20 => "\"tty\" expected",
                21 => "\"tv\" expected",
                22 => "\"n\" expected",
                23 => "\"@\" expected",
                24 => "\"-\" expected",
                25 => "\",\" expected",
                26 => "\"{\" expected",
                27 => "\";\" expected",
                28 => "\"}\" expected",
                29 => "\"+\" expected",
                30 => "\">\" expected",
                31 => "\"~\" expected",
                32 => "\"*\" expected",
                33 => "\"#\" expected",
                34 => "\".\" expected",
                35 => "\"[\" expected",
                36 => "\"=\" expected",
                37 => "\"~=\" expected",
                38 => "\"|=\" expected",
                39 => "\"$=\" expected",
                40 => "\"^=\" expected",
                41 => "\"*=\" expected",
                42 => "\"]\" expected",
                43 => "\":\" expected",
                44 => "\"!\" expected",
                45 => "\"important\" expected",
                46 => "\"/\" expected",
                47 => "\"U\\\\\" expected",
                48 => "\"%\" expected",
                49 => "??? expected",
                50 => "invalid directive",
                51 => "invalid QuotedString",
                52 => "invalid URI",
                53 => "invalid medium",
                54 => "invalid identity",
                55 => "invalid simpleselector",
                56 => "invalid attrib",
                57 => "invalid term",
                58 => "invalid term",
                59 => "invalid term",
                60 => "invalid term",
                61 => "invalid HexValue",
                _ => "error " + n,
            };
            var errorString = string.Format(ErrMsgFormat, line, col, s);
            throw new OpenXmlPowerToolsException(errorString);
        }

        public virtual void SemanticError(int line, int col, string s)
        {
            var errorString = string.Format(ErrMsgFormat, line, col, s);
            throw new OpenXmlPowerToolsException(errorString);
        }

        public virtual void SemanticError(string s)
        {
            throw new OpenXmlPowerToolsException(s);
        }

        public virtual void Warning(int line, int col, string s)
        {
            var errorString = string.Format(ErrMsgFormat, line, col, s);
            throw new OpenXmlPowerToolsException(errorString);
        }

        public virtual void Warning(string s)
        {
            throw new OpenXmlPowerToolsException(s);
        }
    }

    public class CssToken
    {
        public int TokenKind { get; set; }
        public int TokenPositionInBytes { get; set; }
        public int TokenPositionInCharacters { get; set; }
        public int TokenColumn { get; set; }
        public int TokenLine { get; set; }
        public string TokenValue { get; set; }
        public CssToken NextToken { get; set; }
    }

    public class CssBuffer
    {
        public const int EOF = char.MaxValue + 1;
        private const int MIN_BUFFER_LENGTH = 1024;
        private const int MAX_BUFFER_LENGTH = MIN_BUFFER_LENGTH * 64;
        private byte[] m_inputBuffer;
        private int m_bufferStart;
        private int m_bufferLength;
        private int m_inputStreamLength;
        private int m_currentPositionInBuffer;
        private Stream? m_inputStream;
        private readonly bool m_isUserStream;

        public CssBuffer(Stream s, bool isUserStream)
        {
            m_inputStream = s; m_isUserStream = isUserStream;

            if (m_inputStream.CanSeek)
            {
                m_inputStreamLength = (int)m_inputStream.Length;
                m_bufferLength = Math.Min(m_inputStreamLength, MAX_BUFFER_LENGTH);
                m_bufferStart = int.MaxValue;
            }
            else
            {
                m_inputStreamLength = m_bufferLength = m_bufferStart = 0;
            }

            m_inputBuffer = new byte[m_bufferLength > 0 ? m_bufferLength : MIN_BUFFER_LENGTH];
            if (m_inputStreamLength > 0)
            {
                Pos = 0;
            }
            else
            {
                m_currentPositionInBuffer = 0;
            }

            if (m_bufferLength == m_inputStreamLength && m_inputStream.CanSeek)
            {
                Close();
            }
        }

        protected CssBuffer(CssBuffer b)
        {
            m_inputBuffer = b.m_inputBuffer;
            m_bufferStart = b.m_bufferStart;
            m_bufferLength = b.m_bufferLength;
            m_inputStreamLength = b.m_inputStreamLength;
            m_currentPositionInBuffer = b.m_currentPositionInBuffer;
            m_inputStream = b.m_inputStream;
            b.m_inputStream = null;
            m_isUserStream = b.m_isUserStream;
        }

        ~CssBuffer()
        {
            Close();
        }

        protected void Close()
        {
            if (!m_isUserStream && m_inputStream != null)
            {
                m_inputStream.Close();
                m_inputStream = null;
            }
        }

        public virtual int Read()
        {
            if (m_currentPositionInBuffer < m_bufferLength)
            {
                return m_inputBuffer[m_currentPositionInBuffer++];
            }
            else if (Pos < m_inputStreamLength)
            {
                return m_inputBuffer[m_currentPositionInBuffer++];
            }
            else if (m_inputStream != null && !m_inputStream.CanSeek && ReadNextStreamChunk() > 0)
            {
                return m_inputBuffer[m_currentPositionInBuffer++];
            }
            else
            {
                return EOF;
            }
        }

        public int Peek()
        {
            var curPos = Pos;
            var ch = Read();
            Pos = curPos;
            return ch;
        }

        public string GetString(int beg, int end)
        {
            var len = 0;
            var buf = new char[end - beg];
            var oldPos = Pos;
            Pos = beg;
            while (Pos < end)
            {
                buf[len++] = (char)Read();
            }

            Pos = oldPos;
            return new string(buf, 0, len);
        }

        public int Pos
        {
            get => m_currentPositionInBuffer + m_bufferStart;
            set
            {
                if (value >= m_inputStreamLength && m_inputStream != null && !m_inputStream.CanSeek)
                {
                    while (value >= m_inputStreamLength && ReadNextStreamChunk() > 0)
                    { }
                }

                if (value < 0 || value > m_inputStreamLength)
                {
                    throw new ArgumentException("buffer out of bounds access, position: " + value);
                }

                if (value >= m_bufferStart && value < m_bufferStart + m_bufferLength)
                {
                    m_currentPositionInBuffer = value - m_bufferStart;
                }
                else if (m_inputStream != null)
                {
                    m_inputStream.Seek(value, SeekOrigin.Begin);
                    m_bufferLength = m_inputStream.Read(m_inputBuffer, 0, m_inputBuffer.Length);
                    m_bufferStart = value; m_currentPositionInBuffer = 0;
                }
                else
                {
                    m_currentPositionInBuffer = m_inputStreamLength - m_bufferStart;
                }
            }
        }

        private int ReadNextStreamChunk()
        {
            var free = m_inputBuffer.Length - m_bufferLength;
            if (free == 0)
            {
                var newBuf = new byte[m_bufferLength * 2];
                Array.Copy(m_inputBuffer, newBuf, m_bufferLength);
                m_inputBuffer = newBuf;
                free = m_bufferLength;
            }
            var read = m_inputStream.Read(m_inputBuffer, m_bufferLength, free);
            if (read > 0)
            {
                m_inputStreamLength = m_bufferLength += read;
                return read;
            }
            return 0;
        }
    }

    public class HtmlToWmlCssParser : CssBuffer
    {
        public HtmlToWmlCssParser(CssBuffer b) : base(b)
        {
        }

        public override int Read()
        {
            int ch;
            do
            {
                ch = base.Read();
            } while (ch >= 128 && (ch & 0xC0) != 0xC0 && ch != EOF);
            if (ch < 128 || ch == EOF)
            {
                // nothing to do
            }
            else if ((ch & 0xF0) == 0xF0)
            {
                var c1 = ch & 0x07;
                ch = base.Read();
                var c2 = ch & 0x3F;
                ch = base.Read();
                var c3 = ch & 0x3F;
                ch = base.Read();
                var c4 = ch & 0x3F;
                ch = ((c1 << 6 | c2) << 6 | c3) << 6 | c4;
            }
            else if ((ch & 0xE0) == 0xE0)
            {
                var c1 = ch & 0x0F;
                ch = base.Read();
                var c2 = ch & 0x3F;
                ch = base.Read();
                var c3 = ch & 0x3F;
                ch = (c1 << 6 | c2) << 6 | c3;
            }
            else if ((ch & 0xC0) == 0xC0)
            {
                var c1 = ch & 0x1F;
                ch = base.Read();
                var c2 = ch & 0x3F;
                ch = c1 << 6 | c2;
            }
            return ch;
        }
    }

    public class Scanner
    {
        private const char END_OF_LINE = '\n';
        private const int c_eof = 0;
        private const int c_maxT = 49;
        private const int c_noSym = 49;
        private const int c_maxTokenLength = 128;

        public CssBuffer ScannerBuffer { get; set; }

        private CssToken m_currentToken;
        private int m_currentInputCharacter;
        private int m_currentCharacterBytePosition;
        private int m_unicodeCharacterPosition;
        private int m_columnNumberOfCurrentCharacter;
        private int m_lineNumberOfCurrentCharacter;
        private int m_eolInComment;
        private static readonly Hashtable s_start;

        private CssToken m_tokensAlreadyPeeked;
        private CssToken m_currentPeekToken;

        private char[] m_textOfCurrentToken = new char[c_maxTokenLength];
        private int m_lengthOfCurrentToken;

        static Scanner()
        {
            s_start = new Hashtable(128);
            for (var i = 65; i <= 84; ++i)
            {
                s_start[i] = 1;
            }

            for (var i = 86; i <= 90; ++i)
            {
                s_start[i] = 1;
            }

            for (var i = 95; i <= 95; ++i)
            {
                s_start[i] = 1;
            }

            for (var i = 97; i <= 122; ++i)
            {
                s_start[i] = 1;
            }

            for (var i = 10; i <= 10; ++i)
            {
                s_start[i] = 2;
            }

            for (var i = 13; i <= 13; ++i)
            {
                s_start[i] = 2;
            }

            for (var i = 48; i <= 57; ++i)
            {
                s_start[i] = 3;
            }

            for (var i = 9; i <= 9; ++i)
            {
                s_start[i] = 4;
            }

            for (var i = 11; i <= 12; ++i)
            {
                s_start[i] = 4;
            }

            for (var i = 32; i <= 32; ++i)
            {
                s_start[i] = 4;
            }

            s_start[60] = 5;
            s_start[45] = 40;
            s_start[39] = 11;
            s_start[34] = 12;
            s_start[40] = 13;
            s_start[41] = 14;
            s_start[64] = 15;
            s_start[44] = 16;
            s_start[123] = 17;
            s_start[59] = 18;
            s_start[125] = 19;
            s_start[43] = 20;
            s_start[62] = 21;
            s_start[126] = 41;
            s_start[42] = 42;
            s_start[35] = 22;
            s_start[46] = 23;
            s_start[91] = 24;
            s_start[61] = 25;
            s_start[124] = 27;
            s_start[36] = 29;
            s_start[94] = 31;
            s_start[93] = 34;
            s_start[58] = 35;
            s_start[33] = 36;
            s_start[47] = 37;
            s_start[85] = 43;
            s_start[37] = 39;
            s_start[CssBuffer.EOF] = -1;
        }

        public Scanner(string fileName)
        {
            Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            ScannerBuffer = new CssBuffer(stream, false);
            Init();
        }

        public Scanner(Stream s)
        {
            ScannerBuffer = new CssBuffer(s, true);
            Init();
        }

        private void Init()
        {
            m_currentCharacterBytePosition = -1;
            m_lineNumberOfCurrentCharacter = 1;
            m_columnNumberOfCurrentCharacter = 0;
            m_unicodeCharacterPosition = -1;
            m_eolInComment = 0;
            NextCh();
            if (m_currentInputCharacter == 0xEF)
            {
                NextCh();
                var ch1 = m_currentInputCharacter;
                NextCh();
                var ch2 = m_currentInputCharacter;
                if (ch1 != 0xBB || ch2 != 0xBF)
                {
                    throw new NotSupportedException(string.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
                }
                ScannerBuffer = new HtmlToWmlCssParser(ScannerBuffer);
                m_columnNumberOfCurrentCharacter = 0;
                m_unicodeCharacterPosition = -1;
                NextCh();
            }
            m_currentPeekToken = m_tokensAlreadyPeeked = new CssToken();
        }

        private void NextCh()
        {
            if (m_eolInComment > 0)
            {
                m_currentInputCharacter = END_OF_LINE;
                m_eolInComment--;
            }
            else
            {
                m_currentCharacterBytePosition = ScannerBuffer.Pos;
                m_currentInputCharacter = ScannerBuffer.Read();
                m_columnNumberOfCurrentCharacter++;
                m_unicodeCharacterPosition++;
                if (m_currentInputCharacter == '\r' && ScannerBuffer.Peek() != '\n')
                {
                    m_currentInputCharacter = END_OF_LINE;
                }

                if (m_currentInputCharacter == END_OF_LINE)
                {
                    m_lineNumberOfCurrentCharacter++; m_columnNumberOfCurrentCharacter = 0;
                }
            }
        }

        private void AddCh()
        {
            if (m_lengthOfCurrentToken >= m_textOfCurrentToken.Length)
            {
                var newBuf = new char[2 * m_textOfCurrentToken.Length];
                Array.Copy(m_textOfCurrentToken, 0, newBuf, 0, m_textOfCurrentToken.Length);
                m_textOfCurrentToken = newBuf;
            }
            if (m_currentInputCharacter != CssBuffer.EOF)
            {
                m_textOfCurrentToken[m_lengthOfCurrentToken++] = (char)m_currentInputCharacter;
                NextCh();
            }
        }

        private bool Comment0()
        {
            int level = 1, pos0 = m_currentCharacterBytePosition, line0 = m_lineNumberOfCurrentCharacter, col0 = m_columnNumberOfCurrentCharacter, charPos0 = m_unicodeCharacterPosition;
            NextCh();
            if (m_currentInputCharacter == '*')
            {
                NextCh();
                for (; ; )
                {
                    if (m_currentInputCharacter == '*')
                    {
                        NextCh();
                        if (m_currentInputCharacter == '/')
                        {
                            level--;
                            if (level == 0)
                            {
                                m_eolInComment = m_lineNumberOfCurrentCharacter - line0;
                                NextCh();
                                return true;
                            }
                            NextCh();
                        }
                    }
                    else if (m_currentInputCharacter == CssBuffer.EOF)
                    {
                        return false;
                    }
                    else
                    {
                        NextCh();
                    }
                }
            }
            else
            {
                ScannerBuffer.Pos = pos0;
                NextCh();
                m_lineNumberOfCurrentCharacter = line0;
                m_columnNumberOfCurrentCharacter = col0;
                m_unicodeCharacterPosition = charPos0;
            }
            return false;
        }

        private void CheckLiteral()
        {
            switch (m_currentToken.TokenValue)
            {
                case "url":
                    m_currentToken.TokenKind = 9;
                    break;

                case "all":
                    m_currentToken.TokenKind = 12;
                    break;

                case "aural":
                    m_currentToken.TokenKind = 13;
                    break;

                case "braille":
                    m_currentToken.TokenKind = 14;
                    break;

                case "embossed":
                    m_currentToken.TokenKind = 15;
                    break;

                case "handheld":
                    m_currentToken.TokenKind = 16;
                    break;

                case "print":
                    m_currentToken.TokenKind = 17;
                    break;

                case "projection":
                    m_currentToken.TokenKind = 18;
                    break;

                case "screen":
                    m_currentToken.TokenKind = 19;
                    break;

                case "tty":
                    m_currentToken.TokenKind = 20;
                    break;

                case "tv":
                    m_currentToken.TokenKind = 21;
                    break;

                case "n":
                    m_currentToken.TokenKind = 22;
                    break;

                case "important":
                    m_currentToken.TokenKind = 45;
                    break;

                default:
                    break;
            }
        }

        private CssToken NextToken()
        {
            while (m_currentInputCharacter == ' ' || m_currentInputCharacter == 10 || m_currentInputCharacter == 13)
            {
                NextCh();
            }

            if (m_currentInputCharacter == '/' && Comment0())
            {
                return NextToken();
            }

            var recKind = c_noSym;
            var recEnd = m_currentCharacterBytePosition;
            m_currentToken = new CssToken
            {
                TokenPositionInBytes = m_currentCharacterBytePosition,
                TokenColumn = m_columnNumberOfCurrentCharacter,
                TokenLine = m_lineNumberOfCurrentCharacter,
                TokenPositionInCharacters = m_unicodeCharacterPosition
            };
            int state;
            if (s_start.ContainsKey(m_currentInputCharacter))
            {
                state = (int)s_start[m_currentInputCharacter];
            }
            else
            {
                state = 0;
            }
            m_lengthOfCurrentToken = 0;
            AddCh();

#pragma warning disable S907 // "goto" statement should not be used, but switch case is a boarder case and not worth refactoring it
            switch (state)
            {
                case -1:
                    {
                        m_currentToken.TokenKind = c_eof;
                        break;
                    }
                case 0:
                    {
                        if (recKind != c_noSym)
                        {
                            m_lengthOfCurrentToken = recEnd - m_currentToken.TokenPositionInBytes;
                            SetScannerBehindT();
                        }
                        m_currentToken.TokenKind = recKind;
                        break;
                    }
                case 1:
                    while (m_currentInputCharacter == '-' || m_currentInputCharacter >= '0' && m_currentInputCharacter <= '9' || m_currentInputCharacter >= 'A' && m_currentInputCharacter <= 'Z' || m_currentInputCharacter == '_' || m_currentInputCharacter >= 'a' && m_currentInputCharacter <= 'z')
                    {
                        AddCh();
                    }
                    {
                        m_currentToken.TokenKind = 1; m_currentToken.TokenValue = new string(m_textOfCurrentToken, 0, m_lengthOfCurrentToken);
                        CheckLiteral();
                        return m_currentToken;
                    }
                case 2:
                    {
                        m_currentToken.TokenKind = 2;
                        break;
                    }
                case 3:
                    {
                        m_currentToken.TokenKind = 3;
                        break;
                    }
                case 4:
                    {
                        m_currentToken.TokenKind = 4;
                        break;
                    }
                case 5:
                    if (m_currentInputCharacter == '!')
                    {
                        AddCh();

                        goto case 6;
                    }
                    else
                    {
                        goto case 0;
                    }
                case 6:
                    if (m_currentInputCharacter == '-')
                    {
                        AddCh();
                        goto case 7;
                    }
                    else
                    {
                        goto case 0;
                    }
                case 7:
                    if (m_currentInputCharacter == '-')
                    {
                        AddCh();
                        goto case 8;
                    }
                    else
                    {
                        goto case 0;
                    }
                case 8:
                    {
                        m_currentToken.TokenKind = 5;
                        break;
                    }
                case 9:
                    if (m_currentInputCharacter == '>')
                    {
                        AddCh();
                        goto case 10;
                    }
                    else
                    {
                        goto case 0;
                    }
                case 10:
                    {
                        m_currentToken.TokenKind = 6;
                        break;
                    }
                case 11:
                    {
                        m_currentToken.TokenKind = 7;
                        break;
                    }
                case 12:
                    {
                        m_currentToken.TokenKind = 8;
                        break;
                    }
                case 13:
                    {
                        m_currentToken.TokenKind = 10;
                        break;
                    }
                case 14:
                    {
                        m_currentToken.TokenKind = 11;
                        break;
                    }
                case 15:
                    {
                        m_currentToken.TokenKind = 23;
                        break;
                    }
                case 16:
                    {
                        m_currentToken.TokenKind = 25;
                        break;
                    }
                case 17:
                    {
                        m_currentToken.TokenKind = 26;
                        break;
                    }
                case 18:
                    {
                        m_currentToken.TokenKind = 27;
                        break;
                    }
                case 19:
                    {
                        m_currentToken.TokenKind = 28;
                        break;
                    }
                case 20:
                    {
                        m_currentToken.TokenKind = 29;
                        break;
                    }
                case 21:
                    {
                        m_currentToken.TokenKind = 30;
                        break;
                    }
                case 22:
                    {
                        m_currentToken.TokenKind = 33;
                        break;
                    }
                case 23:
                    {
                        m_currentToken.TokenKind = 34;
                        break;
                    }
                case 24:
                    {
                        m_currentToken.TokenKind = 35;
                        break;
                    }
                case 25:
                    {
                        m_currentToken.TokenKind = 36;
                        break;
                    }
                case 26:
                    {
                        m_currentToken.TokenKind = 37;
                        break;
                    }
                case 27:
                    if (m_currentInputCharacter == '=')
                    {
                        AddCh();
                        goto case 28;
                    }
                    else
                    {
                        goto case 0;
                    }
                case 28:
                    {
                        m_currentToken.TokenKind = 38;
                        break;
                    }
                case 29:
                    if (m_currentInputCharacter == '=')
                    {
                        AddCh();
                        goto case 30;
                    }
                    else
                    {
                        goto case 0;
                    }
                case 30:
                    {
                        m_currentToken.TokenKind = 39;
                        break;
                    }
                case 31:
                    if (m_currentInputCharacter == '=')
                    {
                        AddCh();
                        goto case 32;
                    }
                    else
                    {
                        goto case 0;
                    }
                case 32:
                    {
                        m_currentToken.TokenKind = 40;
                        break;
                    }
                case 33:
                    {
                        m_currentToken.TokenKind = 41;
                        break;
                    }
                case 34:
                    {
                        m_currentToken.TokenKind = 42;
                        break;
                    }
                case 35:
                    {
                        m_currentToken.TokenKind = 43;
                        break;
                    }
                case 36:
                    {
                        m_currentToken.TokenKind = 44;
                        break;
                    }
                case 37:
                    {
                        m_currentToken.TokenKind = 46;
                        break;
                    }
                case 38:
                    {
                        m_currentToken.TokenKind = 47;
                        break;
                    }
                case 39:
                    {
                        m_currentToken.TokenKind = 48;
                        break;
                    }
                case 40:
                    recEnd = m_currentCharacterBytePosition;
                    recKind = 24;
                    if (m_currentInputCharacter == '-')
                    {
                        AddCh();
                        goto case 9;
                    }
                    else
                    {
                        m_currentToken.TokenKind = 24;
                        break;
                    }
                case 41:
                    if (m_currentInputCharacter == '=')
                    {
                        AddCh();
                        goto case 26;
                    }
                    else
                    {
                        m_currentToken.TokenKind = 31;
                        break;
                    }
                case 42:
                    if (m_currentInputCharacter == '=')
                    {
                        AddCh();
                        goto case 33;
                    }
                    else
                    {
                        m_currentToken.TokenKind = 32;
                        break;
                    }
                case 43:
                    if (m_currentInputCharacter == '-' || m_currentInputCharacter >= '0' && m_currentInputCharacter <= '9' || m_currentInputCharacter >= 'A' && m_currentInputCharacter <= 'Z' || m_currentInputCharacter == '_' || m_currentInputCharacter >= 'a' && m_currentInputCharacter <= 'z')
                    {
                        AddCh();
                        goto case 1;
                    }
                    else if (m_currentInputCharacter == 92)
                    {
                        AddCh();
                        goto case 38;
                    }
                    else
                    {
                        m_currentToken.TokenKind = 1;
                        m_currentToken.TokenValue = new string(m_textOfCurrentToken, 0, m_lengthOfCurrentToken);
                        CheckLiteral();
                        return m_currentToken;
                    }
            }
#pragma warning restore S907 // "goto" statement should not be used, but switch case is a boarder case and not worth refactoring it

            m_currentToken.TokenValue = new string(m_textOfCurrentToken, 0, m_lengthOfCurrentToken);
            return m_currentToken;
        }

        private void SetScannerBehindT()
        {
            ScannerBuffer.Pos = m_currentToken.TokenPositionInBytes;
            NextCh();
            m_lineNumberOfCurrentCharacter = m_currentToken.TokenLine; m_columnNumberOfCurrentCharacter = m_currentToken.TokenColumn; m_unicodeCharacterPosition = m_currentToken.TokenPositionInCharacters;
            for (var i = 0; i < m_lengthOfCurrentToken; i++)
            {
                NextCh();
            }
        }

        public CssToken Scan()
        {
            if (m_tokensAlreadyPeeked.NextToken == null)
            {
                return NextToken();
            }
            else
            {
                m_currentPeekToken = m_tokensAlreadyPeeked = m_tokensAlreadyPeeked.NextToken;
                return m_tokensAlreadyPeeked;
            }
        }

        public CssToken Peek()
        {
            do
            {
                if (m_currentPeekToken.NextToken == null)
                {
                    m_currentPeekToken.NextToken = NextToken();
                }
                m_currentPeekToken = m_currentPeekToken.NextToken;
            } while (m_currentPeekToken.TokenKind > c_maxT);

            return m_currentPeekToken;
        }

        public void ResetPeek()
        {
            m_currentPeekToken = m_tokensAlreadyPeeked;
        }
    }
}