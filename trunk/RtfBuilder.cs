using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rtf
{
    /// <summary>
    /// Used to construct simply RTF formatted text
    /// See: http://www.biblioscape.com/rtf15_spec.htm for a list of RTF tags
    /// This class help assure that tags are closed properly, escaapes text, manages the font table,
    /// and manages the color table.
    /// </summary>
    class RtfBuilder
    {
        StringBuilder text = new StringBuilder();
        List<Color> colors = new List<Color>();
        List<Font> fonts = new List<Font>();
        Font mDefaultFont;
        int mnTag = 0;

        public string ToRtf(Font defaultFont)
        {
            if (mnTag != 0)
                throw new Exception("unclosed tag in RTF");
            mDefaultFont = defaultFont;
            StringBuilder sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\deff0");
            AppendFontTable(sb);
            AppendColorTable(sb);
            int nFontSize = (int)(defaultFont.SizeInPoints * 2);
            sb.Append(@"{\f0\fs" + nFontSize + " ");
            sb.Append(text.ToString());
            sb.Append("}}");
            return sb.ToString();            
        }
        public override string ToString()
        {
            return text.ToString();
        }
        public void StartTag(string s) 
        { 
            mnTag++; 
            text.Append(@"{\" + s + " "); 
        }
        public void CloseTag() 
        { 
            mnTag--; text.Append("}"); 
        }
        
        public void AddColoredString(Color c, string s)
        {
            SetColor(c);
            text.Append(s);
            CloseTag();
        }

        #region character styles
        public void SetBold() { StartTag("b"); }
        public void SetItalic() { StartTag("i"); }
        public void SetColor(Color c) { StartTag("cf" + GetColorIndex(c)); }
        public void SetHighlight(Color c) { StartTag("cb" + GetColorIndex(c)); }
        public void SetLasVegasLights() { StartTag("animtext1"); }
        public void SetBlinkingBackground() { StartTag("animtext2"); }
        public void SetSparkleText() { StartTag("animtext3"); }
        public void SetMarchingBlackAnts() { StartTag("animtext4"); }
        public void SetMarchingRedAnts() { StartTag("animtext5"); }
        public void SetShimmer() { StartTag("animtext6"); }
        public void SetEmboss() { StartTag("embo"); }
        public void SetEngrave() { StartTag("impr"); }
        public void SetSubscript() { StartTag("sub"); }
        public void SetOutline() { StartTag("outl"); }
        public void SetShadow() { StartTag("shad"); }
        public void SetStrike() { StartTag("strike"); }
        public void SetDblStrike() { StartTag("strikedl"); }
        public void SetUnderline() { StartTag("ul"); }
        public void SetDottedUnderline() { StartTag("uld"); }
        public void SetDashUnderline() { StartTag("uldash"); }
        public void SetDotDashUnderline() { StartTag("uldashd"); }
        public void SetDotDotDashUnderline() { StartTag("uldashdd"); }
        public void SetDoubleUnderline() { StartTag("uldb"); }
        public void SetThickUnderline() { StartTag("ulth"); }
        public void SetWordUnderline() { StartTag("ulw"); }
        public void SetWaveUnderline() { StartTag("ulwave"); }
        public void SetSuperscript() { StartTag("super"); }
        #endregion

        public void AddBoldString(string s)
        {
            SetBold();
            text.Append(s);
            CloseTag();
        }
        public void AddHighlightedString(Color c, string s)
        {
            SetHighlight(c);
            text.Append(s);
            CloseTag();
        }
        public void AddLine(string s)
        {
            AddString(s);
            AddLine();
        }
        public void AddString(string s)
        {
            AddRtf(EscapeText(s));
        }
        public string EscapeText(string s)
        {
            return s.Replace(@"\", @"\").Replace("{", @"\{").Replace("}", @"\}").Replace("\n", "\\par\n");
        }
        public void AddRtf(string s)
        {
            text.Append(s);
        }
        public void AddLine()
        {
            text.Append("\\par\n");
        }
        public void DefineFont(Font font)
        {
            if (fonts.Contains(font)) return;
            fonts.Add(font);
        }
        public void DefineColor(Color color)
        {
            if (colors.Contains(color)) return;
            colors.Add(color);
        }
        private int GetColorIndex(Color color)
        {
            if (!colors.Contains(color))
                DefineColor(color);
            return colors.IndexOf(color) + 1; // color 0 is system defined
        }
        private int GetFontIndex(Font font)
        {
            if (!fonts.Contains(font))
                DefineFont(font);
            return fonts.IndexOf(font);
        }
        private StringBuilder AppendFontTable(StringBuilder sb)
        {
            sb.Append(@"{\fonttbl");
            AppendFontTableEntry(sb, 0, mDefaultFont);
            for (int i = 0; i < fonts.Count; ++i)
                AppendFontTableEntry(sb, i + 1, fonts[i]);
            return sb.Append("}");
        }
        private StringBuilder AppendColorTable(StringBuilder sb)
        {
            sb.Append(@"{\colortbl ;");
            foreach (Color color in colors)
                AppendColorTableEntry(sb, color);
            return sb.Append("}");
        }
        private StringBuilder AppendColorTableEntry(StringBuilder sb, Color color)
        {
            return sb.Append(@"\red").Append(color.R).Append(@"\green").Append(color.G).Append(@"\blue")
                .Append(color.B).Append(";");
        }
        private StringBuilder AppendFontTableEntry(StringBuilder sb, int n, Font font)
        {
            return sb.Append(@"{\f").Append(n).Append(@"\fnil\fcharset0 ").Append(font.Name).Append(";}");
        }
        public void Clear()
        {
            text = new StringBuilder();
        }
        public string GetRtfBody()
        {
            return text.ToString();
        }
    }
}
