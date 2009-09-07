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

using System.Collections.Generic;

namespace GitSharp
{
    public class AlternateRepositoryDatabase : ObjectDatabase
    {
        private readonly Repository _repository;
        private readonly ObjectDatabase _odb;

        public AlternateRepositoryDatabase(Repository alternateRepository)
        {
            _repository = alternateRepository;
            _odb = _repository.ObjectDatabase;
        }

        public Repository getRepository()
        {
            return _repository;
        }

        public override void closeSelf()
        {
            _repository.Close();
        }

		public override void create()
        {
            _repository.Create();
        }

		public override bool exists()
        {
            return _odb.exists();
        }

        public override bool hasObject1(AnyObjectId objectId)
        {
            return _odb.hasObject1(objectId);
        }

        public override bool tryAgain1()
        {
            return _odb.tryAgain1();
        }

        public override bool hasObject2(string objectName)
        {
            return _odb.hasObject2(objectName);
        }

        public override ObjectLoader openObject1(WindowCursor curs, AnyObjectId objectId)
        {
            return _odb.openObject1(curs, objectId);
        }

        public override ObjectLoader openObject2(WindowCursor curs, string objectName, AnyObjectId objectId)
        {
            return _odb.openObject2(curs, objectName, objectId);
        }

		public override void OpenObjectInAllPacksImplementation(ICollection<PackedObjectLoader> @out, WindowCursor windowCursor, AnyObjectId objectId)
        {
			_odb.OpenObjectInAllPacksImplementation(@out, windowCursor, objectId);
        }

        public override ObjectDatabase[] loadAlternates()
        {
            return _odb.getAlternates();
        }

        public override void closeAlternates()
        {
        }
    }
}