/*
 * Copyright (C) 2008, Google Inc.
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

namespace GitSharp.Core.Transport
{

    public class ReceiveCommand
    {
        public enum Type
        {
            CREATE,
            UPDATE,
            UPDATE_NONFASTFORWARD,
            DELETE
        }

        public enum Result
        {
            NOT_ATTEMPTED,
            REJECTED_NOCREATE,
            REJECTED_NODELETE,
            REJECTED_NONFASTFORWARD,
            REJECTED_CURRENT_BRANCH,
            REJECTED_MISSING_OBJECT,
            REJECTED_OTHER_REASON,
            LOCK_FAILURE,
            OK
        }

        private readonly ObjectId oldId;
        private readonly ObjectId newId;
        private string name;
        private Type type;
        private Ref refc;
        private Result status;
        private string message;

        public ReceiveCommand(ObjectId oldId, ObjectId newId, string name)
        {
            this.oldId = oldId;
            this.newId = newId;
            this.name = name;

            type = Type.UPDATE;
            if (ObjectId.ZeroId.Equals(oldId))
                type = Type.CREATE;
            if (ObjectId.ZeroId.Equals(newId))
                type = Type.DELETE;
            status = Result.NOT_ATTEMPTED;
        }

        public ObjectId getOldId()
        {
            return oldId;
        }

        public ObjectId getNewId()
        {
            return newId;
        }

        public string getRefName()
        {
            return name;
        }

        public Type getType()
        {
            return type;
        }

        public Ref getRef()
        {
            return refc;
        }

        public Result getResult()
        {
            return status;
        }

        public string getMessage()
        {
            return message;
        }

        public void setResult(Result s)
        {
            setResult(s, null);
        }

        public void setResult(Result s, string m)
        {
            status = s;
            message = m;
        }

        public void setRef(Ref r)
        {
            refc = r;
        }

        public void setType(Type t)
        {
            type = t;
        }

        public override string ToString()
        {
            return getType() + ": " + getOldId() + " " + getNewId() + " " + getRefName();
        }
    }

}