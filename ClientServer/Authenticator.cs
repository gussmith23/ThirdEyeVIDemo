using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ClientServer
{
    class Authenticator
    {
        private const string DB_FILE_NAME = "auth_users.xml";
        private Dictionary<string, string> mUserDatabase;
        private CryptoManager mCrypto;

        public Authenticator()
        {
            LoadDB();
            mCrypto = new CryptoManager((long)(Math.Pow(2, 32) - 1), (long)(Math.Pow(2, 16) + Math.Pow(2, 22) + Math.Pow(2, 28) + Math.Pow(2, 30) - 4));
        }

        public void LoadDB()
        {
            mUserDatabase = new Dictionary<string, string>();
            XmlDocument xDoc = new XmlDocument();
            if (System.IO.File.Exists(DB_FILE_NAME))
            {
                xDoc.Load(DB_FILE_NAME);
                foreach (XmlNode xRoot in xDoc.ChildNodes)
                {
                    if (String.Compare(xRoot.Name, "xml", true) != 0)
                    {
                        foreach (XmlNode xNode in xRoot.ChildNodes)
                        {
                            if (String.Compare(xNode.Name, "User", true) == 0)
                            {
                                XmlElement xElem = xNode as XmlElement;
                                if (xElem != null)
                                {
                                    if ((xElem.HasAttribute("ID") && xElem.HasAttribute("Token")))
                                    {
                                        string u = xElem.Attributes["ID"].Value;
                                        string p = xElem.Attributes["Token"].Value;
                                        if (!mUserDatabase.ContainsKey(u)) mUserDatabase.Add(u, p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Save();
            }
        }

        public void AddUser(string UserName, string Password)
        {
            if (!mUserDatabase.ContainsKey(UserName))
            {
                string p = EncryptPassword(Password);
                mUserDatabase.Add(UserName, p);
                Save();
            }
        }

        private string EncryptPassword(string Password)
        {
            return UTF8Encoding.UTF8.GetString(mCrypto.Encrypt(UTF8Encoding.UTF8.GetBytes(Password)));
        }

        public void RemoveUser(string UserName)
        {
            if (mUserDatabase.ContainsKey(UserName))
            {
                mUserDatabase.Remove(UserName);
                Save();
            }
        }
        public void Save()
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.AppendChild(xDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlNode xRoot = xDoc.CreateElement("Users");

            foreach(KeyValuePair<string, string> kvp in mUserDatabase)
            {
                XmlElement xElem = xDoc.CreateElement("User");
                xElem.SetAttribute("ID", kvp.Key);
                xElem.SetAttribute("Token", kvp.Value);
                xRoot.AppendChild(xElem);
            }
            xDoc.AppendChild(xRoot);
            xDoc.Save("auth_users.xml");
        }
        public bool Authenticate(string uname, string pword)
        {
            if ((mUserDatabase.Count == 0) && String.IsNullOrEmpty(uname) && String.IsNullOrEmpty(pword))
            {
                return true;
            }
            if (mUserDatabase.ContainsKey(uname))
            {
                return (String.Compare(EncryptPassword(pword), mUserDatabase[uname]) == 0);
            }
            return false;
        }
    }
}
