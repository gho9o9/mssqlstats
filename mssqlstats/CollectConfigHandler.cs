using System.Linq;
using System.Text;
using System.Configuration;

namespace mssqlstats
{
    class CollectConfigHandler : ConfigurationSection
    {
        /// <summary>
        /// servers要素下にhost要素のリストを設定する
        /// </summary>
        [ConfigurationProperty("Queries", IsDefaultCollection = true)]
        public CollectConfigItemCollection Query
        {
            get
            {
                return (CollectConfigItemCollection)this["Queries"];
            }
        }
    }


    /// <summary>
    /// host要素のコレクションをまとめるservers要素の定義
    /// </summary>
    public class CollectConfigItemCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// すべてのキー名のコレクション
        /// </summary>
        public string[] AllKeys
        {
            get
            {
                return (from o in BaseGetAllKeys() select o.ToString()).ToArray();
            }
        }

        /// <summary>
        /// 指定されたキーに対応する要素の情報
        /// </summary>
        /// <param name="name">キー名</param>
        /// <returns>要素</returns>
        public new CollectConfigItem this[string name]
        {
            get
            {
                return (CollectConfigItem)BaseGet(name);
            }
        }


        /// <summary>
        /// 新しい ConfigurationElement を作成
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new CollectConfigItem();
        }

        /// <summary>
        /// 指定した構成要素の要素キーを取得
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            CollectConfigItem item = element as CollectConfigItem;
            return item.Name;
        }
    }

    /// <summary>
    /// host要素の定義
    /// </summary>
    public class CollectConfigItem : ConfigurationElement
    {
        /// <summary>
        /// name属性、必須、キーとして使用する.
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("version", IsRequired = true)]
        public int Version
        {
            get
            {
                return (int)this["version"];
            }
            set
            {
                this["version"] = value;
            }
        }

        /// <summary>
        /// address属性、必須
        /// </summary>
        [ConfigurationProperty("target", IsRequired = true)]
        public string Target
        {
            get
            {
                return (string)this["target"];
            }
            set
            {
                this["target"] = value;
            }
        }

        /// <summary>
        /// port属性、必須
        /// </summary>
        [ConfigurationProperty("run", IsRequired = true)]
        public bool Run
        {
            get
            {
                return (bool)this["run"];
            }
            set
            {
                this["run"] = value;
            }
        }

        /// <summary>
        /// 診断用文字列を返す
        /// </summary>
        /// <returns>診断用文字列</returns>
        public override string ToString()
        {
            var buf = new StringBuilder();
            buf.Append("name").Append(Name);
            buf.Append(", version").Append(Version.ToString());
            buf.Append(", target").Append(Target);
            buf.Append(", run").Append(Run.ToString());
            return buf.ToString();
        }
    }
}
