using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace Ux
{
    public partial class EvalMgr
    {
        public partial class FormulaParser
        {
            // AST节点类型
            private abstract class Node
            {
                public abstract double Evaluate();
                public void Release()
                {
                    OnRelease();
                    Pool.Push(this);
                }
                protected virtual void OnRelease() { }
            }
            //数字节点
            private class NumberNode : Node
            {
                private double _value;
                public NumberNode Init(double value)
                {
                    _value = value;
                    return this;
                }
                public override double Evaluate() => _value;
                protected override void OnRelease()
                {
                    _value = 0;
                }
            }
            //变量节点
            private class VariableNode : Node
            {
                private string _name;
                public VariableNode Init(string name)
                {
                    _name = name;
                    return this;
                }
                public override double Evaluate()
                {
                    if (variables.TryGetValue(_name, out var value))
                    {
                        return value;
                    }
                    if (nameToVariable.TryGetValue(_name, out var func))
                    {
                        return func();
                    }
                    throw new ArgumentException($"未知的变量名: {_name}");
                }
                protected override void OnRelease()
                {
                    _name = null;
                }
            }
            //一元运算符节点
            private class UnaryNode : Node
            {
                private char _op;
                private Node _operand;
                public UnaryNode Init(char op, Node operand)
                {
                    (_op, _operand) = (op, operand);
                    return this;
                }
                public override double Evaluate()
                {
                    double value = _operand.Evaluate();
                    return _op switch
                    {
                        '+' => value,
                        '-' => -value,
                        _ => throw new ArgumentException($"未知的一元运算符: {_op}")
                    };
                }
                protected override void OnRelease()
                {
                    _operand.Release();
                    _operand = null;
                }
            }
            //运算符节点
            private class BinaryNode : Node
            {
                private char _op;
                private Node _left;
                private Node _right;
                public BinaryNode Init(char op, Node left, Node right)
                {
                    (_op, _left, _right) = (op, left, right);
                    return this;
                }
                public override double Evaluate()
                {
                    double left = _left.Evaluate();
                    double right = _right.Evaluate();
                    return _op switch
                    {
                        '+' => left + right,
                        '-' => left - right,
                        '*' => left * right,
                        '/' => right == 0 ? throw new DivideByZeroException("除数不可为0") : left / right,
                        _ => throw new ArgumentException($"未知的运算符: {_op}")
                    };
                }
                protected override void OnRelease()
                {
                    _left.Release();
                    _right.Release();
                    _left = null;
                    _right = null;
                }
            }
            //函数节点
            private class FunctionNode : Node
            {
                private string _name;
                private IList<Node> _arguments;
                static OverdueMap<int, double[]> doubleMap = new OverdueMap<int, double[]>(_timeout);
                public FunctionNode Init(string name, IList<Node> arguments)
                {
                    (_name, _arguments) = (name, arguments);
                    return this;
                }
                public override double Evaluate()
                {
                    if (!_builtinFunctions.TryGetValue(_name, out var func))
                    {
                        if (!nameToDoubleFunc.TryGetValue(_name, out func))
                        {
                            throw new ArgumentException($"未知的函数： {_name}");
                        }
                    }
                    var argCount = _arguments != null ? _arguments.Count : 0;
                    if (!doubleMap.TryGetValue(argCount, out var args))
                    {
                        args = new double[argCount];
                        doubleMap.Add(argCount, args);
                    }
                    for (int i = 0; i < argCount; i++)
                    {
                        args[i] = _arguments[i].Evaluate();
                    }
                    return func(args);
                }
                protected override void OnRelease()
                {
                    _name = null;
                    if (_arguments != null)
                    {
                        foreach (var _arg in _arguments)
                        {
                            _arg.Release();
                        }
                        _arguments.Clear();
                        Pool.Push(_arguments);
                    }
                }
            }

            // 增强的词法分析器
            private class Lexer
            {
                private string _input;
                private int _position;
                private char Current => _position < _input.Length ? _input[_position] : '\0';
                List<Token> _tokens = new List<Token>();


                public void Init(string input)
                {
                    _input = input;
                }
                public void Release()
                {
                    _input = null;
                    _position = 0;
                    _tokens.Clear();
                    Pool.Push(this);
                }

                public List<Token> Tokenize()
                {
                    while (_position < _input.Length)
                    {
                        if (char.IsWhiteSpace(Current))
                        {
                            _position++;
                            continue;
                        }

                        if (char.IsDigit(Current) || Current == '.')
                        {
                            _tokens.Add(ReadNumber());
                        }
                        else if (char.IsLetter(Current) || Current == '_')
                        {
                            _tokens.Add(ReadIdentifier());
                        }
                        else if (Current == '+' || Current == '-' || Current == '*' || Current == '/')
                        {
                            _tokens.Add(new Token(TokenType.Operator, Current));
                            _position++;
                        }
                        else if (Current == '(')
                        {
                            _tokens.Add(new Token(TokenType.LeftParen, Current));
                            _position++;
                        }
                        else if (Current == ')')
                        {
                            _tokens.Add(new Token(TokenType.RightParen, Current));
                            _position++;
                        }
                        else if (Current == ',')
                        {
                            _tokens.Add(new Token(TokenType.Comma, Current));
                            _position++;
                        }
                        else
                        {
                            throw new ArgumentException($"无效字符: '{Current}'=> position {_position}");
                        }
                    }
                    return _tokens;
                }

                private Token ReadNumber()
                {
                    int start = _position;
                    bool hasDecimal = false;

                    while (_position < _input.Length)
                    {
                        if (char.IsDigit(Current))
                        {
                            _position++;
                        }
                        else if (Current == '.' && !hasDecimal)
                        {
                            hasDecimal = true;
                            _position++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    // 使用 Span 避免字符串分配
                    var span = _input.AsSpan(start, _position - start);
                    if (double.TryParse(span, out double value))
                        return new Token(TokenType.Number, value);
                    throw new ArgumentException($"无效数字: '{span.ToString()}'");
                }

                private Token ReadIdentifier()
                {
                    int start = _position;
                    while (_position < _input.Length && (char.IsLetterOrDigit(Current) || Current == '_'))
                    {
                        _position++;
                    }
                    // 使用 Span 避免字符串分配
                    var span = _input.AsSpan(start, _position - start);                    
                    return new Token(TokenType.Identifier, span.ToString());
                }
            }

            private enum TokenType { None, Number, Identifier, Operator, LeftParen, RightParen, Comma }

            private struct Token
            {
                public TokenType Type { get; }

                public char CharValue { get; }
                public string StrValue { get; }
                public double NumValue { get; }

                public Token(TokenType type, string value)
                {
                    (Type, StrValue) = (type, value);
                    CharValue = '\0';
                    NumValue = 0;

                }
                public Token(TokenType type, char value)
                {
                    (Type, CharValue) = (type, value);
                    StrValue = null;
                    NumValue = 0;
                }
                public Token(TokenType type, double value)
                {
                    (Type, NumValue) = (type, value);
                    CharValue = '\0';
                    StrValue = null;
                }
            }


            // 重新设计的语法分析器
            private class Parser
            {
                private List<Token> _tokens;
                private int _position;
                private Token Current => _position < _tokens.Count ? _tokens[_position] : default;

                public void Init(List<Token> tokens)
                {
                    _tokens = tokens;
                }

                public void Release()
                {
                    _tokens = null;
                    _position = 0;
                    Pool.Push(this);
                }
                public Node ParseExpression()
                {
                    var node = ParseAdditive();
                    if (Current.Type != TokenType.None)
                    {
                        // 如果是逗号或右括号，可能是函数参数的一部分，允许继续
                        if (Current.Type != TokenType.Comma && Current.Type != TokenType.RightParen)
                        {
                            throw new ArgumentException($"token 解析错误: {Current}");
                        }
                    }
                    return node;
                }
                //解析加减
                private Node ParseAdditive()
                {
                    var left = ParseMultiplicative();

                    while (Current.Type == TokenType.Operator &&
                           (Current.CharValue == '+' || Current.CharValue == '-'))
                    {
                        var op = Current.CharValue;
                        _position++;
                        var right = ParseMultiplicative();
                        left = Pool.Get<BinaryNode>().Init(op, left, right);
                    }

                    return left;
                }
                //解析乘除
                private Node ParseMultiplicative()
                {
                    var left = ParseUnary();

                    while (Current.Type == TokenType.Operator &&
                           (Current.CharValue == '*' || Current.CharValue == '/'))
                    {
                        var op = Current.CharValue;
                        _position++;
                        var right = ParseUnary();
                        left = Pool.Get<BinaryNode>().Init(op, left, right);
                    }

                    return left;
                }
                //解一元符号，例如-x、+x
                private Node ParseUnary()
                {
                    if (Current.Type == TokenType.Operator &&
                        (Current.CharValue == '+' || Current.CharValue == '-'))
                    {
                        var op = Current.CharValue;
                        _position++;
                        return Pool.Get<UnaryNode>().Init(op, ParsePrimary());
                    }
                    return ParsePrimary();
                }
                //解析基础元素
                private Node ParsePrimary()
                {
                    if (Current.Type == TokenType.None) throw new ArgumentException("解析错误");

                    switch (Current.Type)
                    {
                        case TokenType.Number:
                            return ParseNumber();

                        case TokenType.Identifier:
                            return ParseIdentifierOrFunction();

                        case TokenType.LeftParen:
                            return ParseParenthesizedExpression();

                        default:
                            throw new ArgumentException($"token 解析错误: {Current}");
                    }
                }

                private Node ParseNumber()
                {
                    var num = Current.NumValue;
                    _position++;
                    return Pool.Get<NumberNode>().Init(num);
                }
                private Node ParseIdentifierOrFunction()
                {
                    string name = Current.StrValue;
                    _position++;

                    // 检查是否是函数调用
                    if (Current.Type == TokenType.LeftParen)
                    {
                        _position++; // 跳过 '('                        
                        // 处理无参数情况
                        if (Current.Type == TokenType.RightParen)
                        {
                            _position++; // 跳过 ')'
                            return Pool.Get<FunctionNode>().Init(name, null);
                        }

                        var arguments = Pool.Get<List<Node>>();
                        // 解析参数列表
                        while (true)
                        {
                            // 解析参数表达式
                            arguments.Add(ParseExpression());

                            if (Current.Type == TokenType.None) throw new ArgumentException("函数调用中输入意外结束");

                            if (Current.Type == TokenType.RightParen)
                            {
                                _position++; // 跳过 ')'
                                break;
                            }

                            if (Current.Type != TokenType.Comma)
                                throw new ArgumentException($"非预期的逗号或右括号： {Current}");

                            _position++; // 跳过 ','
                        }

                        return Pool.Get<FunctionNode>().Init(name, arguments);
                    }

                    // 普通变量
                    return Pool.Get<VariableNode>().Init(name);
                }

                private Node ParseParenthesizedExpression()
                {
                    _position++; // 跳过 '('
                    Node node = ParseExpression();

                    if (Current.Type != TokenType.RightParen)
                        throw new ArgumentException("函数调用中输入意外结束");

                    _position++; // 跳过 ')'
                    return node;
                }
            }

            Node _ast;
            public FormulaParser Init(string expression)
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return this;
                }
                var lexer = Pool.Get<Lexer>();
                lexer.Init(expression);
                var parser = Pool.Get<Parser>();
                parser.Init(lexer.Tokenize());
                _ast = parser.ParseExpression();
                parser.Release();
                lexer.Release();
                return this;
            }
            public void Release()
            {
                _ast.Release();
                _ast = null;
                Pool.Push(this);
            }

            public double Evaluate()
            {
                if (_ast == null)
                    return 0;
                return _ast.Evaluate();
            }
        }
    }
}
