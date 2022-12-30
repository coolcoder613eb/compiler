using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace teenytiny
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Teeny Tiny Compiler");

            if (args.Length != 1)
            {
                Console.WriteLine("Error: Compiler needs source file as argument.");
                return;
            }
            string input = File.ReadAllText(args[0]);
            string outFile = args[0].Split('.')[0] + ".c";

            // Initialize the lexer, emitter, and parser.
            Lexer lexer = new Lexer(input);
            Emitter emitter = new Emitter(outFile);
            Parser parser = new Parser(lexer, emitter);

            parser.program(); // Start the parser.
            emitter.writeFile(); // Write the output to file.
            Console.WriteLine("Compiling completed.");
        }
    }

    // Lexer object keeps track of the current character and returns the next token.
    class Lexer
    {
        string source; // Source code to lex as a string. Append a newline to simplify lexing/parsing the last token/statement.
        char curChar; // Current character in the string.
        int curPos; // Current position in the string.

        public Lexer(string input)
        {
            source = input + '\n';
            curChar = '\0';
            curPos = -1;
            nextChar();
        }

        // Process the next character.
        void nextChar()
        {
            curPos += 1;
            if (curPos >= source.Length)
            {
                curChar = '\0'; // EOF
            }
            else
            {
                curChar = source[curPos];
            }
        }

        // Return the lookahead character.
        char peek()
        {
            if (curPos + 1 >= source.Length)
            {
                return '\0';
            }
            return source[curPos + 1];
        }

        // Invalid token found, print error message and exit.
        void abort(string message)
        {
            Console.WriteLine("Lexing error. " + message);
            Environment.Exit(1);
        }

        // Skip whitespace except newlines, which we will use to indicate the end of a statement.
        void skipWhitespace()
        {
            while (curChar == ' ' || curChar == '\t' || curChar == '\r')
            {
                nextChar();
            }
        }

        // Skip comments in the code.
        void skipComment()
        {
            if (curChar == '#')
            {
                while (curChar != '\n')
                {
                    nextChar();
                }
            }
        }

        // Return the next token.
        public Token getToken()
        {
            skipWhitespace();
            skipComment();
            Token token = null;

            // Check the first character of this token to see if we can decide what it is.
            // If it is a multiple character operator (e.g., !=), number, identifier, or keyword then we will process the rest.
            if (curChar == '+')
            {
                token = new Token(curChar.ToString(), TokenType.PLUS);
            }
            else if (curChar == '-')
            {
                token = new Token(curChar.ToString(), TokenType.MINUS);
            }
            else if (curChar == '*')
            {
                token = new Token(curChar.ToString(), TokenType.ASTERISK);
            }
            else if (curChar == '/')
            {
                token = new Token(curChar.ToString(), TokenType.SLASH);
            }
            else if (curChar == '=')
            {
                // Check whether this token is = or ==
                if (peek() == '=')
                {
                    char lastChar = curChar;
                    nextChar();
                    token = new Token(lastChar.ToString() + curChar.ToString(), TokenType.EQEQ);
                }
                else
                {
                    token = new Token(curChar.ToString(), TokenType.EQ);
                }
            }
            else if (curChar == '>')
            {
                // Check whether this is token is > or >=
                if (peek() == '=')
                {
                    char lastChar = curChar;
                    nextChar();
                    token = new Token(lastChar.ToString() + curChar.ToString(), TokenType.GTEQ);
                }
                else
                {
                    token = new Token(curChar.ToString(), TokenType.GT);
                }
            }
            else if (curChar == '<')
            {
                // Check whether this is token is < or <=
                if (peek() == '=')
                {
                    char lastChar = curChar;
                    nextChar();
                    token = new Token(lastChar.ToString() + curChar.ToString(), TokenType.LTEQ);
                }
                else
                {
                    token = new Token(curChar.ToString(), TokenType.LT);
                }
            }
            else if (curChar == '!')
            {
                if (peek() == '=')
                {
                    char lastChar = curChar;
                    nextChar();
                    token = new Token(lastChar.ToString() + curChar.ToString(), TokenType.NOTEQ);
                }
                else
                {
                    abort("Expected !=, got !" + peek());
                }
            }
            else if (curChar == '\"')
            {
                // Get characters between quotations.
                nextChar();
                int startPos = curPos;

                while (curChar != '\"')
                {
                    // Don't allow special characters in the string. No escape characters, newlines, tabs, or %.
                    // We will be using C's printf on this string.
                    if (curChar == '\r' || curChar == '\n' || curChar == '\t' || curChar == '\\' || curChar == '%')
                    {
                        abort("Illegal character in string.");
                    }
                    nextChar();
                }

                string tokText = source.Substring(startPos, curPos - startPos); // Get the substring.
                token = new Token(tokText, TokenType.STRING);
            }
            else if (curChar.ToString().All(char.IsDigit))
            {
                // Leading character is a digit, so this must be a number.
                // Get all consecutive digits and decimal if there is one.
                int startPos = curPos;
                while (peek().ToString().All(char.IsDigit))
                {
                    nextChar();
                }
                if (peek() == '.')
                {
                    nextChar();

                    // Must have at least one digit after decimal.
                    if (!peek().ToString().All(char.IsDigit))
                    {
                        // Error!
                        abort("Illegal character in number.");
                    }
                    while (peek().ToString().All(char.IsDigit))
                    {
                        nextChar();
                    }
                }

                string tokText = source.Substring(startPos, curPos - startPos + 1); // Get the substring.
                token = new Token(tokText, TokenType.NUMBER);
            }
            else if (curChar.ToString().All(char.IsLetter))
            {
                // Leading character is a letter, so this must be an identifier or a keyword.
                // Get all consecutive alpha numeric characters.
                int startPos = curPos;
                while (peek().ToString().All(char.IsLetterOrDigit))
                {
                    nextChar();
                }

                // Check if the token is in the list of keywords.
                string tokText = source.Substring(startPos, curPos - startPos + 1); // Get the substring.
                TokenType? keyword = Token.checkIfKeyword(tokText);
                if (keyword == null)
                {
                    token = new Token(tokText, TokenType.IDENT);
                }
                else
                {
                    token = new Token(tokText, (TokenType)keyword);
                }
            }
            else if (curChar == '\n')
            {
                token = new Token(curChar.ToString(), TokenType.NEWLINE);
            }
            else if (curChar == '\0')
            {
                token = new Token("", TokenType.EOF);
            }
            else
            {
                // Unknown token!
                abort("Unknown token: " + curChar);
            }

            nextChar();
            return token;
        }
    }

    // Token contains the original text and the type of token.
    class Token
    {
        public string text; // The token's actual text. Used for identifiers, strings, and numbers.
        public TokenType kind; // The TokenType that this token is classified as.

        public Token(string tokenText, TokenType tokenKind)
        {
            text = tokenText;
            kind = tokenKind;
        }

        public static TokenType? checkIfKeyword(string tokenText)
        {
            foreach (TokenType kind in Enum.GetValues(typeof(TokenType)))
            {
                // Relies on all keyword enum values being 1XX.
                if (kind.ToString() == tokenText && (int)kind >= 100 && (int)kind < 200)
                {
                    return kind;
                }
            }
            return null;
        }
    }

    // TokenType is our enum for all the types of tokens.
    enum TokenType
    {
        EOF = -1,
        NEWLINE = 0,
        NUMBER = 1,
        IDENT = 2,
        STRING = 3,
        // Keywords.
        LABEL = 101,
        GOTO = 102,
        PRINT = 103,
        INPUT = 104,
        LET = 105,
        IF = 106,
        THEN = 107,
        ENDIF = 108,
        WHILE = 109,
        REPEAT = 110,
        ENDWHILE = 111,
        REM = 112,
        // Operators.
        EQ = 201,
        PLUS = 202,
        MINUS = 203,
        ASTERISK = 204,
        SLASH = 205,
        EQEQ = 206,
        NOTEQ = 207,
        LT = 208,
        LTEQ = 209,
        GT = 210,
        GTEQ = 211
    }
    // Parser object keeps track of current token and checks if the code matches the grammar.
    class Parser
    {
        Lexer lexer;
        Emitter emitter;

        HashSet<string> symbols; // All variables we have declared so far.
        HashSet<string> labelsDeclared; // Keep track of all labels declared
        HashSet<string> labelsGotoed; // All labels goto'ed, so we know if they exist or not.

        Token curToken;
        Token peekToken;

        public Parser(Lexer lexer, Emitter emitter)
        {
            this.lexer = lexer;
            this.emitter = emitter;

            symbols = new HashSet<string>();
            labelsDeclared = new HashSet<string>();
            labelsGotoed = new HashSet<string>();

            curToken = null;
            peekToken = null;
            nextToken();
            nextToken(); // Call this twice to initialize current and peek.
        }

        // Return true if the current token matches.
        bool checkToken(TokenType kind)
        {
            return kind == curToken.kind;
        }

        // Return true if the next token matches.
        bool checkPeek(TokenType kind)
        {
            return kind == peekToken.kind;
        }

        // Try to match current token. If not, error. Advances the current token.
        void match(TokenType kind)
        {
            if (!checkToken(kind))
                abort("Expected " + kind.ToString() + ", got " + curToken.kind.ToString());
            nextToken();
        }

        // Advances the current token.
        void nextToken()
        {
            curToken = peekToken;
            peekToken = lexer.getToken();
            // No need to worry about passing the EOF, lexer handles that.
        }

        void abort(string message)
        {
            Console.WriteLine("Error. " + message);
            Environment.Exit(1);
        }

        // Production rules.

        // program ::= {statement}
        public void program()
        {
            emitter.headerLine("#include <stdio.h>");
            emitter.headerLine("int main(void){");

            // Since some newlines are required in our grammar, need to skip the excess.
            while (checkToken(TokenType.NEWLINE))
                nextToken();

            // Parse all the statements in the program.
            while (!checkToken(TokenType.EOF))
                statement();

            // Wrap things up.
            emitter.emitLine("return 0;");
            emitter.emitLine("}");

            // Check that each label referenced in a GOTO is declared.
            foreach (string label in labelsGotoed)
                if (!labelsDeclared.Contains(label))
                    abort("Attempting to GOTO to undeclared label: " + label);
        }

        // One of the following statements...
        void statement()
        {
            // Check the first token to see what kind of statement this is.
            // "REM"
            if (checkToken(TokenType.REM))
            {
                string comment_text = "// ";
                List<string> comment_words = new List<string>();
                while (!checkToken(TokenType.NEWLINE))
                {
                    nextToken();
                    comment_words.Add(curToken.text);
                }
                comment_text += string.Join(" ", comment_words);
                emitter.emitLine(comment_text);
            }

            // "PRINT" (expression | string)
            else if (checkToken(TokenType.PRINT))
            {
                nextToken();

                if (checkToken(TokenType.STRING))
                {
                    // Simple string, so print it.
                    emitter.emitLine("printf(\"" + curToken.text + "\\n\");");
                    nextToken();
                }
                else
                {
                    // Expect an expression and print the result as a float.
                    emitter.emit("printf(\"%" + ".2f\\n\", (float)(");
                    expression();
                    emitter.emitLine("));");
                }
            }

            // "IF" comparison "THEN" block "ENDIF"
            else if (checkToken(TokenType.IF))
            {
                nextToken();
                emitter.emit("if(");
                comparison();

                match(TokenType.THEN);
                nl();
                emitter.emitLine("){");

                // Zero or more statements in the body.
                while (!checkToken(TokenType.ENDIF))
                    statement();

                match(TokenType.ENDIF);
                emitter.emitLine("}");
            }

            // "WHILE" comparison "REPEAT" block "ENDWHILE"
            else if (checkToken(TokenType.WHILE))
            {
                nextToken();
                emitter.emit("while(");
                comparison();

                match(TokenType.REPEAT);
                nl();
                emitter.emitLine("){");

                // Zero or more statements in the loop body.
                while (!checkToken(TokenType.ENDWHILE))
                    statement();

                match(TokenType.ENDWHILE);
                emitter.emitLine("}");
            }
            // "LABEL" ident
            else if (checkToken(TokenType.LABEL))
            {
                nextToken();

                // Make sure this label doesn't already exist.
                if (labelsDeclared.Contains(curToken.text))
                    abort("Label already exists: " + curToken.text);
                labelsDeclared.Add(curToken.text);

                emitter.emitLine(curToken.text + ":");
                match(TokenType.IDENT);
            }

            // "GOTO" ident
            else if (checkToken(TokenType.GOTO))
            {
                nextToken();
                labelsGotoed.Add(curToken.text);
                emitter.emitLine("goto " + curToken.text + ";");
                match(TokenType.IDENT);
            }

            // "LET" ident = expression
            else if (checkToken(TokenType.LET))
            {
                nextToken();

                //  Check if ident exists in symbol table. If not, declare it.
                if (!symbols.Contains(curToken.text))
                {
                    symbols.Add(curToken.text);
                    emitter.headerLine("float " + curToken.text + ";");
                }

                emitter.emit(curToken.text + " = ");
                match(TokenType.IDENT);
                match(TokenType.EQ);

                expression();
                emitter.emitLine(";");
            }

            // "INPUT" ident
            else if (checkToken(TokenType.INPUT))
            {
                nextToken();

                // If variable doesn't already exist, declare it.
                if (!symbols.Contains(curToken.text))
                {
                    symbols.Add(curToken.text);
                    emitter.headerLine("float " + curToken.text + ";");
                }

                // Emit scanf but also validate the input. If invalid, set the variable to 0 and clear the input.
                emitter.emitLine("if(0 == scanf(\"%" + "f\", &" + curToken.text + ")) {");
                emitter.emitLine(curToken.text + " = 0;");
                emitter.emit("scanf(\"%");
                emitter.emitLine("*s\");");
                emitter.emitLine("}");
                match(TokenType.IDENT);
            }

            // Newline.
            nl();
        }

        // nl ::= '\n'+
        void nl()
        {
            //print("NEWLINE");

            // Require at least one newline.
            match(TokenType.NEWLINE);
            // But we will allow extra newlines too, of course.
            while (checkToken(TokenType.NEWLINE))
                nextToken();
        }

        // comparison ::= expression (("==" | "!=" | ">" | ">=" | "<" | "<=") expression)+
        void comparison()
        {
            expression();
            // Must be at least one comparison operator and another expression.
            if (isComparisonOperator())
            {
                emitter.emit(curToken.text);
                nextToken();
                expression();
            }
            // Can have 0 or more comparison operator and expressions.
            while (isComparisonOperator())
            {
                emitter.emit(curToken.text);
                nextToken();
                expression();
            }
        }

        // Return true if the current token is a comparison operator.
        bool isComparisonOperator()
        {
            return checkToken(TokenType.GT) || checkToken(TokenType.GTEQ) || checkToken(TokenType.LT) || checkToken(TokenType.LTEQ) || checkToken(TokenType.EQEQ) || checkToken(TokenType.NOTEQ);
        }

        // expression ::= term {( "-" | "+" ) term}
        void expression()
        {
            term();
            // Can have 0 or more +/- and expressions.
            while (checkToken(TokenType.PLUS) || checkToken(TokenType.MINUS))
            {
                emitter.emit(curToken.text);
                nextToken();
                term();
            }
        }

        // term ::= unary {( "/" | "*" ) unary}
        void term()
        {
            unary();
            // Can have 0 or more *// and expressions.
            while (checkToken(TokenType.ASTERISK) || checkToken(TokenType.SLASH))
            {
                emitter.emit(curToken.text);
                nextToken();
                unary();
            }
        }

        // unary ::= ["+" | "-"] primary
        void unary()
        {
            // Optional unary +/-
            if (checkToken(TokenType.PLUS) || checkToken(TokenType.MINUS))
            {
                emitter.emit(curToken.text);
                nextToken();
            }
            primary();
        }

        // primary ::= number | ident
        void primary()
        {
            if (checkToken(TokenType.NUMBER))
            {
                emitter.emit(curToken.text);
                nextToken();
            }
            else if (checkToken(TokenType.IDENT))
            {
                // Ensure the variable already exists.
                if (!symbols.Contains(curToken.text))
                    abort("Referencing variable before assignment: " + curToken.text);

                emitter.emit(curToken.text);
                nextToken();
            }
            else
            {
                // Error!
                abort("Unexpected token at " + curToken.text);
            }
        }
    }
class Emitter
    {
        string fullPath;
        string header;
        string code;

        public Emitter(string fullPath)
        {
            this.fullPath = fullPath;
            header = "";
            code = "";
        }

        public void emit(string code)
        {
            this.code += code;
        }

        public void emitLine(string code)
        {
            this.code += code + '\n';
        }

        public void headerLine(string code)
        {
            header += code + '\n';
        }

        public void writeFile()
        {
            using (StreamWriter outputFile = new StreamWriter(fullPath))
            {
                outputFile.Write(header + code);
            }
        }
    }
}