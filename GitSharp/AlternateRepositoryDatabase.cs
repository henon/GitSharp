/*
 * Copyright (C) 2009, Google Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace GitSharp
{

    public class AlternateRepositoryDatabase : ObjectDatabase
    {
        private readonly Repository repository;
        private readonly ObjectDatabase odb;

        public AlternateRepositoryDatabase(Repository alt)
        {
            repository = alt;
            odb = repository.getObjectDatabase();
        }

        public Repository getRepository()
        {
            return repository;
        }

        public new void closeSelf()
        {
            repository.Close();
        }

        public new void create()
        {
            repository.Create();
        }

        public new bool exists()
        {
            return odb.exists();
        }

        public override bool hasObject1(AnyObjectId objectId)
        {
            return odb.hasObject1(objectId);
        }

        public override bool tryAgain1()
        {
            return odb.tryAgain1();
        }

        public override bool hasObject2(string objectName)
        {
            return odb.hasObject2(objectName);
        }

        public override ObjectLoader openObject1(WindowCursor curs, AnyObjectId objectId)
        {
            return odb.openObject1(curs, objectId);
        }

        public override ObjectLoader openObject2(WindowCursor curs, string objectName, AnyObjectId objectId)
        {
            return odb.openObject2(curs, objectName, objectId);
        }

        public override void openObjectInAllPacks1(System.Collections.Generic.List<PackedObjectLoader> @out, WindowCursor curs, AnyObjectId objectId)
        {
            odb.openObjectInAllPacks1(@out, curs, objectId);
        }

        public override ObjectDatabase[] loadAlternates()
        {
            return odb.getAlternates();
        }

        public override void closeAlternates()
        {
            
        }
    }

}