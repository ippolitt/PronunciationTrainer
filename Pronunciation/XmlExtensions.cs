using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Pronunciation
{
    public static class XmlExtensions
    {
        public static void ScrollToStartTag(this XmlReader reader, string tagName)
        {
            ScrollToStartTag(reader, tagName, false);
        }

        public static bool ScrollToOptionalStartTag(this XmlReader reader, string tagName)
        {
            return ScrollToOptionalStartTag(reader, tagName, false);
        }

        public static void ScrollToEndTag(this XmlReader reader, string tagName)
        {
            ScrollToEndTag(reader, tagName, false);
        }

        public static void ScrollToStartTag(this XmlReader reader, string tagName, bool validateCurrentTag)
        {
            if (ScrollToOptionalStartTag(reader, tagName, validateCurrentTag))
                return;

            throw new ArgumentException();
        }

        public static bool ScrollToOptionalStartTag(this XmlReader reader, string tagName, bool validateCurrentTag)
        {
            if (validateCurrentTag && reader.NodeType == XmlNodeType.Element && reader.Name == tagName)
                return true;

            ScrollToNonWhitespace(reader);
            if (reader.NodeType == XmlNodeType.Element && reader.Name == tagName)
                return true;

            return false;
        }

        public static void ScrollToEndTag(this XmlReader reader, string tagName, bool validateCurrentTag)
        {
            if (validateCurrentTag && reader.NodeType == XmlNodeType.EndElement && reader.Name == tagName)
                return;

            ScrollToNonWhitespace(reader);
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == tagName)
                return;

            throw new ArgumentException();
        }

        public static void ScrollToRootTag(this XmlReader reader, string tagName)
        {
            while (!reader.EOF)
            {
                reader.Read();
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == tagName && reader.Depth == 0)
                        return;

                    throw new ArgumentException();
                }
            }

            throw new ArgumentException();
        }

        public static void ScrollToNonWhitespace(this XmlReader reader)
        {
            while (!reader.EOF)
            {
                reader.Read();
                if (reader.NodeType != XmlNodeType.Whitespace)
                    return;
            }

            throw new ArgumentException();
        }
    }
}
