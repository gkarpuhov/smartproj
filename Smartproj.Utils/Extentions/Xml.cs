using System.Xml;

namespace Smartproj.Utils
{
    public static class XmlEx
    {
        public static string EvalToString(this XmlDocument _doc, string _xpath)
        {
            if (_doc != null)
            {
                var node = _doc.SelectSingleNode(_xpath);
                if (node != null)
                {
                    return node.InnerText;
                }
            }
            return "";
        }
        public static bool TryEvalToInt(this XmlDocument _doc, string _xpath, out int _ret)
        {
            _ret = -1;
            if (_doc != null)
            {
                var node = _doc.SelectSingleNode(_xpath);
                if (node != null)
                {
                    return int.TryParse(node.InnerText, out _ret);
                }
            }
            return false;
        }
        public static bool TryEvalToFloat(this XmlDocument _doc, string _xpath, out float _ret)
        {
            _ret = -1;
            if (_doc != null)
            {
                var node = _doc.SelectSingleNode(_xpath);
                if (node != null)
                {
                    return float.TryParse(node.InnerText, out _ret);
                }
            }
            return false;
        }

    }
}
