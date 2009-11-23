using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace GitSharp
{
    /// <summary>
    /// Represents repository-, user-, and global-configuration for git
    /// </summary>
    public class Config : IEnumerable<KeyValuePair<string, string>>
    {
        private Repository _repo;

        public Config(Repository repo)
        {
            Debug.Assert(repo != null);
            _repo = repo;
        }

        /// <summary>
        /// Direct config access via git style names (i.e. "user.name")
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                var config = _repo._internal_repo.Config;
                var token = key.Split('.');
                
                if (token.Count() == 2)
                {
                    return config.getString(token[0], null, token[1]);
                }

                if (token.Count() == 3)
                {
                    return config.getString(token[0], token[1], token[2]);
                }

                return null;
            }
            set
            {
                var config = _repo._internal_repo.Config;
                var token = key.Split('.');
                if (token.Count() == 2)
                    config.setString(token[0], null, token[1], value);
                else if (token.Count() == 3)
                    config.setString(token[0], token[1], token[2], value);
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var pair in this)
                    yield return pair.Key;
            }
        }

        public IEnumerable<string> Values
        {
            get
            {
                foreach (var pair in this)
                    yield return pair.Value;
            }
        }

        #region IEnumerable<KeyValuePair<string,string>> Members

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            var config = _repo._internal_repo.Config;
            config.getState();
            foreach (var entry in config._state.EntryList)
            {
                if (string.IsNullOrEmpty(entry.name))
                    continue;
                var subsec = (entry.subsection != null ? "." + entry.subsection : "");
                yield return new KeyValuePair<string, string>(entry.section + subsec + "." + entry.name, entry.value);
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Saves the config to the file system.
        /// </summary>
        public void Persist()
        {
            _repo._internal_repo.Config.save();
        }
    }
}
