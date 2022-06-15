using System;

namespace ScrapingTemplateSimple4._8.Models
{
    public class KnownException : Exception
    {
        public KnownException(string s) : base(s)
        {

        }
    }
}