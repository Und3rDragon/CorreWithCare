using System;
using System.Collections.Generic;
using System.Globalization;
using CorreWithCare.Utils;

namespace CorreWithCare.Core;

/// <summary>
/// Corre Expression：一门轻量的表达式语言。
/// <para>运算符：+ - * / （四则）、% 取模、%% 取余、^ 幂、// 浮点除法、
/// &amp;&amp; 且、|| 或、! 非、== != &gt; &lt; &gt;= &lt;= 比较、= 赋值。</para>
/// <para>引用：xxx 为 flag，$xxx 为 counter，$$xxx 为 slider。</para>
/// <para>函数：sin[x] cos[x] tan[x] pow[x,y] log[n,x] ln[x] if[条件,a,b]；常量：pi、e、tao。</para>
/// <para>整数运算使用 int，带小数的运算统一使用 double；布尔结果按 1 / 0 输出。
/// 不含 flag / counter / slider 引用且不含赋值的表达式视为静态表达式，构造时求值并写入 Value；
/// 其余表达式在每次读取时重新求值。</para>
/// </summary>
public class CorreExpression
{
    /// <summary>
    /// 原始表达式文本。
    /// </summary>
    public string Expression;

    /// <summary>
    /// 是否解析成功。
    /// </summary>
    public bool Valid;

    /// <summary>
    /// 缓存值，静态表达式在构造时写入；动态表达式与赋值表达式保持 null。
    /// </summary>
    public double? Value = null;

    /// <summary>
    /// 是否为静态表达式。
    /// </summary>
    public bool Static => Value != null;

    /// <summary>
    /// 是否为赋值表达式。
    /// </summary>
    public bool Assign;

    private Node root;

    private static readonly Dictionary<string, Node> ParseCache = new();

    /// <summary>
    /// 由表达式文本创建。
    /// </summary>
    public CorreExpression(string expression)
    {
        Expression = expression ?? string.Empty;

        if (!Expression.HasValidContent())
        {
            return;
        }

        try
        {
            root = GetOrParse(Expression);
        }
        catch
        {
            root = null;
            return;
        }

        Valid = true;
        Assign = root is AssignNode;

        // 赋值表达式有副作用，构造时不求值
        if (Assign)
        {
            return;
        }

        if (root.IsStatic)
        {
            Value = root.Eval().AsDouble;
        }
    }

    /// <summary>
    /// 读取当前值：静态表达式返回缓存，其余表达式重新求值。
    /// </summary>
    public double GetValue()
    {
        if (Value.HasValue)
        {
            return Value.Value;
        }

        return root == null ? 0d : root.Eval().AsDouble;
    }

    /// <summary>
    /// 清空解析缓存。
    /// </summary>
    public static void ClearParseCache() => ParseCache.Clear();

    private static Node GetOrParse(string expression)
    {
        if (ParseCache.TryGetValue(expression, out Node node))
        {
            return node;
        }

        node = new ExpressionParser(expression).Parse();
        ParseCache[expression] = node;
        return node;
    }

    private static int ToInt(double value)
    {
        if (value > int.MaxValue) return int.MaxValue;
        if (value < int.MinValue) return int.MinValue;
        return (int)value;
    }

    // ───────────────────────────── 值 ─────────────────────────────

    private enum ValueKind
    {
        Integer,
        Bool,
        Double
    }

    private readonly struct Val
    {
        public readonly ValueKind Kind;
        public readonly int I;
        public readonly double D;
        public readonly bool B;

        private Val(ValueKind kind, int i, double d, bool b)
        {
            Kind = kind;
            I = i;
            D = d;
            B = b;
        }

        public static Val Int(int value) => new Val(ValueKind.Integer, value, value, false);
        public static Val Double(double value) => new Val(ValueKind.Double, 0, value, false);
        public static Val Bool(bool value) => new Val(ValueKind.Bool, 0, 0d, value);

        public bool IsDouble => Kind == ValueKind.Double;

        public int AsInt => Kind switch
        {
            ValueKind.Double => ToInt(D),
            ValueKind.Bool => B ? 1 : 0,
            _ => I
        };

        public double AsDouble => Kind switch
        {
            ValueKind.Double => D,
            ValueKind.Bool => B ? 1d : 0d,
            _ => I
        };

        public bool AsBool => Kind switch
        {
            ValueKind.Double => D != 0d,
            ValueKind.Bool => B,
            _ => I != 0
        };
    }

    // ───────────────────────────── 语法树 ─────────────────────────────

    private abstract class Node
    {
        public abstract bool IsStatic { get; }
        public abstract ValueKind Kind { get; }
        public abstract Val Eval();
    }

    private sealed class LiteralNode : Node
    {
        private readonly Val value;

        public LiteralNode(Val value) => this.value = value;

        public override bool IsStatic => true;
        public override ValueKind Kind => value.Kind;
        public override Val Eval() => value;
    }

    private sealed class VariableNode : Node
    {
        public enum VariableKind { Flag, Counter, Slider }

        private readonly VariableKind kind;
        private readonly string name;

        public VariableNode(VariableKind kind, string name)
        {
            this.kind = kind;
            this.name = name;
        }

        public VariableKind VariableType => kind;
        public string Name => name;

        public override bool IsStatic => false;

        public override ValueKind Kind => kind switch
        {
            VariableKind.Flag => ValueKind.Bool,
            VariableKind.Counter => ValueKind.Integer,
            _ => ValueKind.Double
        };

        public override Val Eval() => kind switch
        {
            VariableKind.Flag => Val.Bool(name.GetFlag()),
            VariableKind.Counter => Val.Int(name.GetCounter()),
            _ => Val.Double(name.GetSlider())
        };
    }

    private sealed class UnaryNode : Node
    {
        public enum UnaryOperator { Not, Negate, Plus }

        private readonly UnaryOperator op;
        private readonly Node operand;

        public UnaryNode(UnaryOperator op, Node operand)
        {
            this.op = op;
            this.operand = operand;
        }

        public override bool IsStatic => operand.IsStatic;

        public override ValueKind Kind => op == UnaryOperator.Not ? ValueKind.Bool : operand.Kind;

        public override Val Eval()
        {
            Val value = operand.Eval();

            return op switch
            {
                UnaryOperator.Not => Val.Bool(!value.AsBool),
                UnaryOperator.Negate => value.IsDouble
                    ? Val.Double(-value.AsDouble)
                    : Val.Int(ToInt(-(double)value.AsInt)),
                _ => value
            };
        }
    }

    private sealed class BinaryNode : Node
    {
        public enum BinaryOperator
        {
            Add, Sub, Mul, Div, FloatDiv, Mod, Rem, Pow,
            Eq, Ne, Gt, Lt, Ge, Le,
            And, Or
        }

        private readonly BinaryOperator op;
        private readonly Node left;
        private readonly Node right;

        public BinaryNode(BinaryOperator op, Node left, Node right)
        {
            this.op = op;
            this.left = left;
            this.right = right;
        }

        public override bool IsStatic => left.IsStatic && right.IsStatic;

        public override ValueKind Kind
        {
            get
            {
                switch (op)
                {
                    case BinaryOperator.Eq:
                    case BinaryOperator.Ne:
                    case BinaryOperator.Gt:
                    case BinaryOperator.Lt:
                    case BinaryOperator.Ge:
                    case BinaryOperator.Le:
                    case BinaryOperator.And:
                    case BinaryOperator.Or:
                        return ValueKind.Bool;

                    case BinaryOperator.Pow:
                    case BinaryOperator.FloatDiv:
                        return ValueKind.Double;

                    default:
                        bool leftInt = left.Kind != ValueKind.Double;
                        bool rightInt = right.Kind != ValueKind.Double;
                        return leftInt && rightInt ? ValueKind.Integer : ValueKind.Double;
                }
            }
        }

        public override Val Eval()
        {
            switch (op)
            {
                case BinaryOperator.And:
                    return Val.Bool(left.Eval().AsBool && right.Eval().AsBool);

                case BinaryOperator.Or:
                    return Val.Bool(left.Eval().AsBool || right.Eval().AsBool);
            }

            Val a = left.Eval();
            Val b = right.Eval();

            switch (op)
            {
                case BinaryOperator.Eq:
                case BinaryOperator.Ne:
                case BinaryOperator.Gt:
                case BinaryOperator.Lt:
                case BinaryOperator.Ge:
                case BinaryOperator.Le:
                    return Compare(op, a, b);

                default:
                    return Arithmetic(op, a, b);
            }
        }

        private static Val Compare(BinaryOperator op, Val left, Val right)
        {
            if (left.IsDouble || right.IsDouble)
            {
                double x = left.AsDouble;
                double y = right.AsDouble;
                const double epsilon = 1e-6;

                return op switch
                {
                    BinaryOperator.Eq => Val.Bool(Math.Abs(x - y) <= epsilon),
                    BinaryOperator.Ne => Val.Bool(Math.Abs(x - y) > epsilon),
                    BinaryOperator.Gt => Val.Bool(x > y),
                    BinaryOperator.Lt => Val.Bool(x < y),
                    BinaryOperator.Ge => Val.Bool(x >= y),
                    _ => Val.Bool(x <= y)
                };
            }

            int i = left.AsInt;
            int j = right.AsInt;

            return op switch
            {
                BinaryOperator.Eq => Val.Bool(i == j),
                BinaryOperator.Ne => Val.Bool(i != j),
                BinaryOperator.Gt => Val.Bool(i > j),
                BinaryOperator.Lt => Val.Bool(i < j),
                BinaryOperator.Ge => Val.Bool(i >= j),
                _ => Val.Bool(i <= j)
            };
        }

        private static Val Arithmetic(BinaryOperator op, Val left, Val right)
        {
            bool anyDouble = left.IsDouble || right.IsDouble;

            switch (op)
            {
                case BinaryOperator.Pow:
                    return Val.Double(Math.Pow(left.AsDouble, right.AsDouble));

                case BinaryOperator.FloatDiv:
                {
                    double divisor = right.AsDouble;
                    return Val.Double(divisor == 0d ? 0d : left.AsDouble / divisor);
                }

                case BinaryOperator.Div:
                {
                    if (anyDouble)
                    {
                        double divisor = right.AsDouble;
                        return Val.Double(divisor == 0d ? 0d : left.AsDouble / divisor);
                    }

                    int divisorInt = right.AsInt;
                    return Val.Int(divisorInt == 0 ? 0 : ToInt((double)left.AsInt / divisorInt));
                }

                case BinaryOperator.Mod:
                {
                    if (anyDouble)
                    {
                        double divisor = right.AsDouble;
                        if (divisor == 0d) return Val.Double(0d);

                        double x = left.AsDouble;
                        return Val.Double(x - divisor * Math.Floor(x / divisor));
                    }

                    int divisorInt = right.AsInt;
                    if (divisorInt == 0) return Val.Int(0);

                    long result = (left.AsInt % divisorInt + divisorInt) % divisorInt;
                    return Val.Int(ToInt(result));
                }

                case BinaryOperator.Rem:
                {
                    if (anyDouble)
                    {
                        double divisor = right.AsDouble;
                        return Val.Double(divisor == 0d ? 0d : left.AsDouble % divisor);
                    }

                    int divisorInt = right.AsInt;
                    return Val.Int(divisorInt == 0 ? 0 : left.AsInt % divisorInt);
                }

                case BinaryOperator.Add:
                    return anyDouble
                        ? Val.Double(left.AsDouble + right.AsDouble)
                        : Val.Int(ToInt((double)left.AsInt + right.AsInt));

                case BinaryOperator.Sub:
                    return anyDouble
                        ? Val.Double(left.AsDouble - right.AsDouble)
                        : Val.Int(ToInt((double)left.AsInt - right.AsInt));

                default:
                    return anyDouble
                        ? Val.Double(left.AsDouble * right.AsDouble)
                        : Val.Int(ToInt((double)left.AsInt * right.AsInt));
            }
        }
    }

    private sealed class FunctionNode : Node
    {
        public enum FunctionKind { Sin, Cos, Tan, Pow, Log, Ln, If }

        private readonly FunctionKind kind;
        private readonly List<Node> arguments;

        public FunctionNode(FunctionKind kind, List<Node> arguments)
        {
            this.kind = kind;
            this.arguments = arguments;
        }

        public override bool IsStatic
        {
            get
            {
                foreach (Node argument in arguments)
                {
                    if (!argument.IsStatic) return false;
                }

                return true;
            }
        }

        public override ValueKind Kind => kind == FunctionKind.If
            ? CommonKind(arguments[1].Kind, arguments[2].Kind)
            : ValueKind.Double;

        public override Val Eval()
        {
            if (kind == FunctionKind.If)
            {
                Val chosen = arguments[0].Eval().AsBool ? arguments[1].Eval() : arguments[2].Eval();

                return CommonKind(arguments[1].Kind, arguments[2].Kind) switch
                {
                    ValueKind.Double => Val.Double(chosen.AsDouble),
                    ValueKind.Bool => Val.Bool(chosen.AsBool),
                    _ => Val.Int(chosen.AsInt)
                };
            }

            return kind switch
            {
                FunctionKind.Sin => Val.Double(Math.Sin(arguments[0].Eval().AsDouble)),
                FunctionKind.Cos => Val.Double(Math.Cos(arguments[0].Eval().AsDouble)),
                FunctionKind.Tan => Val.Double(Math.Tan(arguments[0].Eval().AsDouble)),
                FunctionKind.Pow => Val.Double(Math.Pow(arguments[0].Eval().AsDouble, arguments[1].Eval().AsDouble)),
                FunctionKind.Log => Val.Double(Math.Log(arguments[0].Eval().AsDouble, arguments[1].Eval().AsDouble)),
                _ => Val.Double(Math.Log(arguments[0].Eval().AsDouble))
            };
        }

        /// <summary>
        /// 两个分支合并后的值类型：任一是 Double 取 Double，都是 Bool 取 Bool，其余取 Integer。
        /// </summary>
        private static ValueKind CommonKind(ValueKind left, ValueKind right)
        {
            if (left == ValueKind.Double || right == ValueKind.Double) return ValueKind.Double;
            if (left == ValueKind.Bool && right == ValueKind.Bool) return ValueKind.Bool;

            return ValueKind.Integer;
        }
    }

    private sealed class AssignNode : Node
    {
        private readonly VariableNode target;
        private readonly Node value;

        public AssignNode(VariableNode target, Node value)
        {
            this.target = target;
            this.value = value;
        }

        public override bool IsStatic => false;

        public override ValueKind Kind => value.Kind;

        public override Val Eval()
        {
            Val result = value.Eval();

            switch (target.VariableType)
            {
                case VariableNode.VariableKind.Flag:
                    target.Name.SetFlag(result.AsBool);
                    break;

                case VariableNode.VariableKind.Counter:
                    target.Name.SetCounter(result.AsInt);
                    break;

                default:
                    target.Name.SetSlider((float)result.AsDouble);
                    break;
            }

            return result;
        }
    }

    // ───────────────────────────── 词法 ─────────────────────────────

    private enum TokenKind
    {
        Number, Name, Counter, Slider,
        Plus, Minus, Star, Slash, FloatSlash, Percent, Remainder, Caret,
        And, Or, Not,
        EqEq, NotEq, Gt, Lt, Ge, Le,
        Assign,
        LeftParen, RightParen, LeftBracket, RightBracket, Comma,
        End
    }

    private readonly struct Token
    {
        public readonly TokenKind Kind;
        public readonly string Text;
        public readonly int IntValue;
        public readonly double DoubleValue;
        public readonly bool IsDouble;

        public Token(TokenKind kind, string text = null, int intValue = 0, double doubleValue = 0d, bool isDouble = false)
        {
            Kind = kind;
            Text = text;
            IntValue = intValue;
            DoubleValue = doubleValue;
            IsDouble = isDouble;
        }
    }

    private sealed class Lexer
    {
        private readonly string text;
        private int position;

        public Lexer(string text)
        {
            this.text = text ?? string.Empty;
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            while (position < text.Length)
            {
                char c = text[position];

                if (char.IsWhiteSpace(c))
                {
                    position++;
                    continue;
                }

                if (char.IsDigit(c) || c == '.')
                {
                    tokens.Add(ReadNumber());
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    tokens.Add(new Token(TokenKind.Name, ReadName()));
                    continue;
                }

                if (c == '$')
                {
                    position++;

                    if (position < text.Length && text[position] == '$')
                    {
                        position++;
                        tokens.Add(new Token(TokenKind.Slider, ReadName()));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenKind.Counter, ReadName()));
                    }

                    continue;
                }

                switch (c)
                {
                    case '+':
                        tokens.Add(new Token(TokenKind.Plus));
                        position++;
                        break;

                    case '-':
                        tokens.Add(new Token(TokenKind.Minus));
                        position++;
                        break;

                    case '*':
                        tokens.Add(new Token(TokenKind.Star));
                        position++;
                        break;

                    case '/':
                        position++;

                        if (position < text.Length && text[position] == '/')
                        {
                            position++;
                            tokens.Add(new Token(TokenKind.FloatSlash));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenKind.Slash));
                        }

                        break;

                    case '%':
                        position++;

                        if (position < text.Length && text[position] == '%')
                        {
                            position++;
                            tokens.Add(new Token(TokenKind.Remainder));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenKind.Percent));
                        }

                        break;

                    case '^':
                        tokens.Add(new Token(TokenKind.Caret));
                        position++;
                        break;

                    case '&':
                        position++;

                        if (position >= text.Length || text[position] != '&')
                        {
                            throw new FormatException($"位置 {position - 1}：'&' 必须写成 '&&'。");
                        }

                        position++;
                        tokens.Add(new Token(TokenKind.And));
                        break;

                    case '|':
                        position++;

                        if (position >= text.Length || text[position] != '|')
                        {
                            throw new FormatException($"位置 {position - 1}：'|' 必须写成 '||'。");
                        }

                        position++;
                        tokens.Add(new Token(TokenKind.Or));
                        break;

                    case '!':
                        position++;

                        if (position < text.Length && text[position] == '=')
                        {
                            position++;
                            tokens.Add(new Token(TokenKind.NotEq));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenKind.Not));
                        }

                        break;

                    case '=':
                        position++;

                        if (position < text.Length && text[position] == '=')
                        {
                            position++;
                            tokens.Add(new Token(TokenKind.EqEq));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenKind.Assign));
                        }

                        break;

                    case '>':
                        position++;

                        if (position < text.Length && text[position] == '=')
                        {
                            position++;
                            tokens.Add(new Token(TokenKind.Ge));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenKind.Gt));
                        }

                        break;

                    case '<':
                        position++;

                        if (position < text.Length && text[position] == '=')
                        {
                            position++;
                            tokens.Add(new Token(TokenKind.Le));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenKind.Lt));
                        }

                        break;

                    case '(':
                        tokens.Add(new Token(TokenKind.LeftParen));
                        position++;
                        break;

                    case ')':
                        tokens.Add(new Token(TokenKind.RightParen));
                        position++;
                        break;

                    case '[':
                        tokens.Add(new Token(TokenKind.LeftBracket));
                        position++;
                        break;

                    case ']':
                        tokens.Add(new Token(TokenKind.RightBracket));
                        position++;
                        break;

                    case ',':
                        tokens.Add(new Token(TokenKind.Comma));
                        position++;
                        break;

                    default:
                        throw new FormatException($"位置 {position}：无法识别的字符 '{c}'。");
                }
            }

            tokens.Add(new Token(TokenKind.End));
            return tokens;
        }

        private Token ReadNumber()
        {
            int start = position;
            bool hasDot = false;

            while (position < text.Length)
            {
                char c = text[position];

                if (char.IsDigit(c) || c == '_')
                {
                    position++;
                    continue;
                }

                if (c == '.')
                {
                    if (hasDot) break;

                    hasDot = true;
                    position++;
                    continue;
                }

                break;
            }

            string raw = text.Substring(start, position - start).Replace("_", "");

            if (!hasDot && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return new Token(TokenKind.Number, raw, intValue);
            }

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                throw new FormatException($"位置 {start}：无法解析数字 '{raw}'。");
            }

            return new Token(TokenKind.Number, raw, 0, doubleValue, true);
        }

        private string ReadName()
        {
            int start = position;

            while (position < text.Length)
            {
                char c = text[position];

                if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
                {
                    position++;
                    continue;
                }

                break;
            }

            if (start == position)
            {
                throw new FormatException($"位置 {position}：缺少名称。");
            }

            return text.Substring(start, position - start);
        }
    }

    // ───────────────────────────── 语法 ─────────────────────────────

    private sealed class ExpressionParser
    {
        private readonly List<Token> tokens;
        private int index;

        public ExpressionParser(string expression)
        {
            tokens = new Lexer(expression).Tokenize();
        }

        public Node Parse()
        {
            Node node = ParseAssign();

            if (Current.Kind != TokenKind.End)
            {
                throw new FormatException($"位置 {index}：表达式末尾有多余内容 '{Current.Text ?? Current.Kind.ToString()}'。");
            }

            return node;
        }

        private Token Current => tokens[index];

        private bool Match(TokenKind kind)
        {
            if (Current.Kind != kind) return false;

            index++;
            return true;
        }

        private void Expect(TokenKind kind, string message)
        {
            if (Current.Kind != kind)
            {
                throw new FormatException($"位置 {index}：{message}");
            }

            index++;
        }

        // 最低优先级：赋值（右结合）
        private Node ParseAssign()
        {
            Node left = ParseOr();

            if (Match(TokenKind.Assign))
            {
                if (left is not VariableNode target)
                {
                    throw new FormatException("赋值左侧必须是 flag、$counter 或 $$slider。");
                }

                Node right = ParseAssign();
                return new AssignNode(target, right);
            }

            return left;
        }

        private Node ParseOr()
        {
            Node left = ParseAnd();

            while (Match(TokenKind.Or))
            {
                left = new BinaryNode(BinaryNode.BinaryOperator.Or, left, ParseAnd());
            }

            return left;
        }

        private Node ParseAnd()
        {
            Node left = ParseComparison();

            while (Match(TokenKind.And))
            {
                left = new BinaryNode(BinaryNode.BinaryOperator.And, left, ParseComparison());
            }

            return left;
        }

        // 比较不支持连写：a < b < c 非法
        private Node ParseComparison()
        {
            Node left = ParseAdd();

            BinaryNode.BinaryOperator op;

            switch (Current.Kind)
            {
                case TokenKind.EqEq: op = BinaryNode.BinaryOperator.Eq; break;
                case TokenKind.NotEq: op = BinaryNode.BinaryOperator.Ne; break;
                case TokenKind.Gt: op = BinaryNode.BinaryOperator.Gt; break;
                case TokenKind.Lt: op = BinaryNode.BinaryOperator.Lt; break;
                case TokenKind.Ge: op = BinaryNode.BinaryOperator.Ge; break;
                case TokenKind.Le: op = BinaryNode.BinaryOperator.Le; break;
                default: return left;
            }

            index++;
            return new BinaryNode(op, left, ParseAdd());
        }

        private Node ParseAdd()
        {
            Node left = ParseMultiply();

            while (true)
            {
                if (Match(TokenKind.Plus))
                {
                    left = new BinaryNode(BinaryNode.BinaryOperator.Add, left, ParseMultiply());
                }
                else if (Match(TokenKind.Minus))
                {
                    left = new BinaryNode(BinaryNode.BinaryOperator.Sub, left, ParseMultiply());
                }
                else
                {
                    return left;
                }
            }
        }

        private Node ParseMultiply()
        {
            Node left = ParseUnary();

            while (true)
            {
                if (Match(TokenKind.Star))
                {
                    left = new BinaryNode(BinaryNode.BinaryOperator.Mul, left, ParseUnary());
                }
                else if (Match(TokenKind.Slash))
                {
                    left = new BinaryNode(BinaryNode.BinaryOperator.Div, left, ParseUnary());
                }
                else if (Match(TokenKind.FloatSlash))
                {
                    left = new BinaryNode(BinaryNode.BinaryOperator.FloatDiv, left, ParseUnary());
                }
                else if (Match(TokenKind.Percent))
                {
                    left = new BinaryNode(BinaryNode.BinaryOperator.Mod, left, ParseUnary());
                }
                else if (Match(TokenKind.Remainder))
                {
                    left = new BinaryNode(BinaryNode.BinaryOperator.Rem, left, ParseUnary());
                }
                else
                {
                    return left;
                }
            }
        }

        // 一元优先级高于幂：-2^2 = -(2^2)
        private Node ParseUnary()
        {
            if (Match(TokenKind.Not))
            {
                return new UnaryNode(UnaryNode.UnaryOperator.Not, ParseUnary());
            }

            if (Match(TokenKind.Minus))
            {
                return new UnaryNode(UnaryNode.UnaryOperator.Negate, ParseUnary());
            }

            if (Match(TokenKind.Plus))
            {
                return ParseUnary();
            }

            return ParsePower();
        }

        // 幂右结合：2^3^2 = 2^(3^2)
        private Node ParsePower()
        {
            Node left = ParsePrimary();

            if (Match(TokenKind.Caret))
            {
                return new BinaryNode(BinaryNode.BinaryOperator.Pow, left, ParseUnary());
            }

            return left;
        }

        private Node ParsePrimary()
        {
            Token token = Current;

            switch (token.Kind)
            {
                case TokenKind.Number:
                    index++;
                    return new LiteralNode(token.IsDouble ? Val.Double(token.DoubleValue) : Val.Int(token.IntValue));

                case TokenKind.Counter:
                    index++;
                    return new VariableNode(VariableNode.VariableKind.Counter, token.Text);

                case TokenKind.Slider:
                    index++;
                    return new VariableNode(VariableNode.VariableKind.Slider, token.Text);

                case TokenKind.Name:
                    index++;

                    if (Current.Kind == TokenKind.LeftBracket)
                    {
                        return ParseFunction(token.Text);
                    }

                    if (TryGetConstant(token.Text, out Val constant))
                    {
                        return new LiteralNode(constant);
                    }

                    return new VariableNode(VariableNode.VariableKind.Flag, token.Text);

                case TokenKind.LeftParen:
                    index++;
                    Node inner = ParseAssign();
                    Expect(TokenKind.RightParen, "缺少 ')'。");
                    return inner;
            }

            throw new FormatException($"位置 {index}：这里需要数字、名称或 '('。");
        }

        private Node ParseFunction(string name)
        {
            Expect(TokenKind.LeftBracket, "缺少 '['。");

            List<Node> arguments = new List<Node>();

            if (Current.Kind != TokenKind.RightBracket)
            {
                arguments.Add(ParseAssign());

                while (Match(TokenKind.Comma))
                {
                    arguments.Add(ParseAssign());
                }
            }

            Expect(TokenKind.RightBracket, "缺少 ']'。");

            FunctionNode.FunctionKind kind;

            switch (name.ToLowerInvariant())
            {
                case "sin":
                    kind = FunctionNode.FunctionKind.Sin;
                    break;
                case "cos":
                    kind = FunctionNode.FunctionKind.Cos;
                    break;
                case "tan":
                    kind = FunctionNode.FunctionKind.Tan;
                    break;
                case "pow":
                    kind = FunctionNode.FunctionKind.Pow;
                    break;
                case "log":
                    kind = FunctionNode.FunctionKind.Log;
                    break;
                case "ln":
                    kind = FunctionNode.FunctionKind.Ln;
                    break;
                case "if":
                    kind = FunctionNode.FunctionKind.If;
                    break;
                default:
                    throw new FormatException($"未知函数 '{name}'。");
            }

            int required = kind switch
            {
                FunctionNode.FunctionKind.Pow or FunctionNode.FunctionKind.Log => 2,
                FunctionNode.FunctionKind.If => 3,
                _ => 1
            };

            if (arguments.Count != required)
            {
                throw new FormatException($"函数 '{name}' 需要 {required} 个参数，实际给了 {arguments.Count} 个。");
            }

            return new FunctionNode(kind, arguments);
        }

        private static bool TryGetConstant(string name, out Val value)
        {
            switch (name.ToLowerInvariant())
            {
                case "pi":
                    value = Val.Double(Math.PI);
                    return true;

                case "e":
                    value = Val.Double(Math.E);
                    return true;

                case "tao":
                case "tau":
                    value = Val.Double(Math.PI * 2d);
                    return true;
            }

            value = default;
            return false;
        }
    }
}

public static class CorreExpressionUtils
{
    /// <summary>
    /// 由表达式文本创建 CorreExpression。
    /// </summary>
    public static CorreExpression CreateCorreExpression(this string expression)
    {
        return new CorreExpression(expression);
    }
}
