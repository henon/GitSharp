using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace Gitty.Lib
{
    [Complete]
    public abstract class ObjectLoader
    {
        private ObjectId _id;
        public ObjectId Id {
            get
            {
                if(_id == null){
                    SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
                    
                    using (StreamWriter writer = new StreamWriter(new MemoryStream()))
                    {
                        writer.Write(this.ObjectType.ToString().ToLower());
                        writer.Write((byte) ' ');
                        writer.Write(this.Size.ToString());
                        writer.Write((byte)0);
                        writer.Write(this.CachedBytes);
                        writer.BaseStream.Seek(0, SeekOrigin.Begin);
                        _id = ObjectId.FromRaw(sha.ComputeHash(writer.BaseStream));
                    }
                }
                return _id;
            }
            set
            {
                if (_id != null)
                    throw new InvalidOperationException("Id already set.");
                _id = value;
            }
        }
		
		// I'm not entirely sure how Java's protected works, but it seems
		// this method was callable from a class that didn't inherit from this one,
		// so instead I've marked this as internal for now, though I think
		// public would probably be better - NR
        internal bool HasComputedId  
        {
            get
            {
                return _id != null;
            }
        }

        public abstract ObjectType ObjectType { get; }
        public abstract long Size { get;  }
        public abstract byte[] Bytes { get; }
        public abstract byte[] CachedBytes { get;  }
        public abstract ObjectType RawType { get; }
        public abstract long RawSize { get;  }
        
    }
}
