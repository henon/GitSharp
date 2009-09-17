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
    public sealed class AlternateRepositoryDatabase : ObjectDatabase
    {
        private readonly Repository _repository;
        private readonly ObjectDatabase _objectDatabase;

        public AlternateRepositoryDatabase(Repository alternateRepository)
        {
            _repository = alternateRepository;
            _objectDatabase = _repository.ObjectDatabase;
        }

    	public Repository Repository
    	{
    		get { return _repository; }
    	}

    	public override void CloseSelf()
        {
            _repository.Close();
        }

		public override void Create()
        {
            _repository.Create();
        }

		public override bool Exists()
        {
            return _objectDatabase.Exists();
        }

		protected internal override bool HasObject1(AnyObjectId objectId)
        {
            return _objectDatabase.HasObject1(objectId);
        }

		protected internal override bool TryAgain1()
        {
            return _objectDatabase.TryAgain1();
        }

		protected internal override bool HasObject2(string objectName)
        {
            return _objectDatabase.HasObject2(objectName);
        }

		protected internal override ObjectLoader OpenObject1(WindowCursor curs, AnyObjectId objectId)
        {
            return _objectDatabase.OpenObject1(curs, objectId);
        }

		protected internal override ObjectLoader OpenObject2(WindowCursor curs, string objectName, AnyObjectId objectId)
        {
            return _objectDatabase.OpenObject2(curs, objectName, objectId);
        }

		public override void OpenObjectInAllPacks1(ICollection<PackedObjectLoader> @out, WindowCursor windowCursor, AnyObjectId objectId)
        {
			_objectDatabase.OpenObjectInAllPacks1(@out, windowCursor, objectId);
        }

    	protected override ObjectDatabase[] LoadAlternates()
        {
            return _objectDatabase.GetAlternates();
        }

        public override void CloseAlternates()
        {
        }
    }
}