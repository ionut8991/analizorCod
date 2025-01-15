using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Text;

namespace analizorCod
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeLegend(); 
        }

        private void InitializeLegend()
        {
            
            Label legendLabel = new Label();
            legendLabel.Text = "Legendă:\n" +
                             "%LIT: Literă\n" +
                             "%CIF: Cifră\n" +
                             "%OP: Operator\n" +
                             "%DL: Delimitator\n" +
                             "%STR: Literal string\n" +
                             "%VERBSTR: Literal verbatim string\n" +
                             "%BL: Spațiu\n" +
                             "Identificator: identificator de variabile, funcții etc\n" +
                             "Cuvant cheie: cuvânt cheie specific limbajului\n" +
                             "Operator aritmetic: + - * / %\n" +
                             "Operator relational: = ! > <\n" +
                             "Operator atribuire: =\n" +
                            "Literal intreg: numar intreg\n" +
                            "Literal real: numar real\n" +
                             "Necunoscut: simboluri nerecunoscute";

            legendLabel.AutoSize = true; 
            legendLabel.MaximumSize = new System.Drawing.Size(tabControl1.Width - 20, 0); 
            legendLabel.Location = new System.Drawing.Point(10, 10); 

            tabControl1.TabPages[0].Controls.Add(legendLabel);

        }

        private void btnAnalizeaza_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear(); // Clear transliterare listbox
            listBox3.Items.Clear(); // Clear explorare listbox
            string codSursa = textBox1.Text;

            // Transliterare
            var transliteratedCode = AnalizorLexical.Transliterate(codSursa);
            foreach (var charInfo in transliteratedCode)
            {
                listBox2.Items.Add($"Char: '{charInfo.Value}', Type: {charInfo.Type}");
            }


            // Explorare
            var exploredTokens = AnalizorLexical.Explore(transliteratedCode);
            foreach (var token in exploredTokens)
            {
                listBox3.Items.Add($"Pos: {token.pos}, Val: '{token.Value}', Type:{token.Type} ");
            }

            // Selectare
            var tokens = AnalizorLexical.Select(exploredTokens);
            foreach (var token in tokens)
            {
                listBox1.Items.Add($"Tip: {token.Tip}, Valoare: {token.Valoare}");
            }

            tabControl1.SelectedIndex = 0;
            
        }


        private void btnSterge_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            listBox3.Items.Clear();

        }
    }

    public class CharInfo
    {
        public char Value;
        public string Type;
        public CharInfo(char value, string type)
        {
            this.Value = value;
            this.Type = type;
        }
    }

    public class Token
    {
        public string Tip { get; set; }
        public string Valoare { get; set; }
        public Token(string tip, string valoare)
        {
            Tip = tip;
            Valoare = valoare;
        }
    }
    public class TokenExp
    {
        public int pos;
        public string Value;
        public string Type;
        public TokenExp(int pos, string value, string type)
        {
            this.pos = pos;
            this.Value = value;
            this.Type = type;
        }
    }

    public class AnalizorLexical
    {

        public static List<CharInfo> Transliterate(string codSursa)
        {
            List<CharInfo> transliterated = new List<CharInfo>();
            foreach (char c in codSursa)
            {
                string type;
                if (char.IsLetter(c))
                    type = "%LIT";
                else if (char.IsDigit(c))
                    type = "%CIF";
                else if (Regex.IsMatch(c.ToString(), @"[\+\-\*\/\%=\!><]"))
                    type = "%OP";
                else if (Regex.IsMatch(c.ToString(), @"[;(){}\[\]\.,]"))
                    type = "%DL";
                else if (c == '"')
                    type = "%STR";
                else if (c == '@')
                    type = "%VERBSTR";
                else if (char.IsWhiteSpace(c))
                    type = "%BL";
                else
                    type = "Necunoscut";
                transliterated.Add(new CharInfo(c, type));
            }
            return transliterated;
        }

        public static List<TokenExp> Explore(List<CharInfo> transliterated)
        {
            List<TokenExp> exploredTokens = new List<TokenExp>();
            int pozCurenta = 0;

            while (pozCurenta < transliterated.Count)
            {

                CharInfo citeste() { if (pozCurenta < transliterated.Count) return transliterated[pozCurenta]; else return null; }
                void avanseaza() { pozCurenta++; }
                void ignora_spatii() { while (citeste() != null && citeste().Type == "%BL") avanseaza(); }
                ignora_spatii();
                if (citeste() == null) break;


                if (citeste().Type == "%LIT" || citeste().Value == '_')
                {
                    string sirIdentificator = "";
                    int posStart = pozCurenta;
                    while (citeste() != null && (citeste().Type == "%LIT" || citeste().Type == "%CIF" || citeste().Value == '_'))
                    {
                        sirIdentificator += citeste().Value;
                        avanseaza();
                    }
                    exploredTokens.Add(new TokenExp(posStart, sirIdentificator, "Identificator"));

                }

                else if (citeste().Type == "%OP")
                {
                    string sirOperator = "";
                    int posStart = pozCurenta;
                    while (citeste() != null && citeste().Type == "%OP")
                    {
                        sirOperator += citeste().Value;
                        avanseaza();
                    }
                    exploredTokens.Add(new TokenExp(posStart, sirOperator, "Operator"));

                }
                else if (citeste().Type == "%DL")
                {
                    exploredTokens.Add(new TokenExp(pozCurenta, citeste().Value.ToString(), "Delimitator"));
                    avanseaza();
                }
                else if (citeste().Type == "%STR")
                {
                    string sir = "\"";
                    int posStart = pozCurenta;
                    avanseaza();
                    while (citeste() != null && citeste().Value != '"' && citeste().Value != '\n' && citeste().Value != '\0')
                    {
                        sir += citeste().Value;
                        avanseaza();
                    }
                    if (citeste() != null && citeste().Value == '"')
                    {
                        sir += "\"";
                        avanseaza();
                    }
                    exploredTokens.Add(new TokenExp(posStart, sir, "Literal string"));
                }
                else if (citeste().Type == "%VERBSTR")
                {
                    string sir = "@";
                    int posStart = pozCurenta;
                    avanseaza();
                    if (citeste() != null && citeste().Value == '"')
                    {
                        sir += "\"";
                        avanseaza();
                        while (citeste() != null && citeste().Value != '"' && citeste().Value != '\0')
                        {
                            sir += citeste().Value;
                            avanseaza();
                        }
                        if (citeste() != null && citeste().Value == '"')
                        {
                            sir += "\"";
                            avanseaza();
                        }

                        exploredTokens.Add(new TokenExp(posStart, sir, "Literal verbatim string"));

                    }
                    else
                        exploredTokens.Add(new TokenExp(pozCurenta, "@", "Necunoscut"));

                }

                else if (citeste().Type == "%CIF")
                {
                    string numarLiteral = "";
                    int posStart = pozCurenta;
                    while (citeste() != null && (citeste().Type == "%CIF" || citeste().Value == '.' || citeste().Value == '-' || citeste().Value == 'e' || citeste().Value == 'E'))
                    {
                        numarLiteral += citeste().Value;
                        avanseaza();
                    }
                    exploredTokens.Add(new TokenExp(posStart, numarLiteral, "Literal numeric"));

                }
                else if (citeste() != null)
                {
                    exploredTokens.Add(new TokenExp(pozCurenta, citeste().Value.ToString(), "Necunoscut"));
                    avanseaza();
                }
                else
                    break;


            }
            return exploredTokens;
        }

        public static List<Token> Select(List<TokenExp> exploredTokens)
        {
            List<Token> listaTokenuri = new List<Token>();
            foreach (var tokenExp in exploredTokens)
            {
                if (tokenExp.Type == "Identificator")
                {
                    string[] cuvinteCheie = { "if", "else", "for", "while", "class", "int", "float", "return", "public", "private", "void", "bool", "char", "string", "double", "decimal", "long", "short", "byte", "sbyte", "object", "static", "new", "using", "namespace", "delegate" };
                    if (cuvinteCheie.Contains(tokenExp.Value))
                        listaTokenuri.Add(new Token("Cuvant cheie", tokenExp.Value));
                    else
                        listaTokenuri.Add(new Token("Identificator", tokenExp.Value));
                }
                else if (tokenExp.Type == "Operator")
                {
                    if (Regex.IsMatch(tokenExp.Value, @"[\+\-\*\/\%]"))
                        listaTokenuri.Add(new Token("Operator aritmetic", tokenExp.Value));
                    else if (Regex.IsMatch(tokenExp.Value, @"[\=\!><]+"))
                        listaTokenuri.Add(new Token("Operator relational", tokenExp.Value));
                    else if (Regex.IsMatch(tokenExp.Value, @"[\=]+"))
                        listaTokenuri.Add(new Token("Operator atribuire", tokenExp.Value));
                    else
                        listaTokenuri.Add(new Token("Operator", tokenExp.Value));

                }
                else if (tokenExp.Type == "Delimitator")
                    listaTokenuri.Add(new Token("Delimitator", tokenExp.Value));
                else if (tokenExp.Type == "Literal string")
                    listaTokenuri.Add(new Token("Literal string", tokenExp.Value));
                else if (tokenExp.Type == "Literal verbatim string")
                    listaTokenuri.Add(new Token("Literal verbatim string", tokenExp.Value));
                else if (tokenExp.Type == "Literal numeric")
                {
                    if (Regex.IsMatch(tokenExp.Value, @"^[0-9]+\.[0-9]+[fFmMdD]?$"))
                        listaTokenuri.Add(new Token("Literal real", tokenExp.Value));
                    else if (Regex.IsMatch(tokenExp.Value, @"^[0-9]+[fFmMdD]?$"))
                        listaTokenuri.Add(new Token("Literal intreg", tokenExp.Value));
                    else
                        listaTokenuri.Add(new Token("Necunoscut", tokenExp.Value));
                }
                else
                    listaTokenuri.Add(new Token("Necunoscut", tokenExp.Value));
            }
            return listaTokenuri;
        }
    }
}