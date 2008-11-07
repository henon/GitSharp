using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public class Ref
    {
        public sealed class Storage
        {
            public static readonly Storage New = new Storage(true, false);
            public static readonly Storage Loose = new Storage(true, false);
            public static readonly Storage Packed = new Storage(false, true);
            public static readonly Storage LoosePacked = new Storage(true, true);
            public static readonly Storage Network = new Storage(false, false);

            public bool IsLoose { get; private set; }
            public bool IsPacked { get; private set; }

            private Storage(bool loose, bool packed)
            {
                this.IsLoose = loose;
                this.IsPacked = packed;
            }

            static Storage()
            {
                Loose = new Storage(true, false);
            }
        }

        public Ref(Storage storage, string refName, ObjectId id )
        {
            this.StorageFormat = storage;
            this.Name = refName;
            this.ObjectId = id;
        }

        public Ref(Storage storage, string refName, ObjectId id, ObjectId peeledObjectId)
            : this(storage, refName, id)
        {
            this.PeeledObjectId = peeledObjectId;
        }

        public string Name { get; private set; }
        public Storage StorageFormat { get; private set; }
        public ObjectId ObjectId { get; private set; }
        public ObjectId PeeledObjectId { get; private set; }

        public override string ToString()
        {
            return "Ref[" + Name + "=" + this.ObjectId.ToString() + "]";
        }
    }
}
