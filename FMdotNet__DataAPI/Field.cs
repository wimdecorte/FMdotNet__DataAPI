using System;
using System.Collections.Generic;

namespace FMdotNet__DataAPI
{
    /// <summary>
    /// Represents a FileMaker field and its attributes
    /// </summary>
    public class Field
	{
        internal int fmsVersion { get; set; }

		internal string fieldName { get; set; }
		internal string tableOccurance { get; set; }
		internal int repetitionNumber { get; set; } // nullable
		internal string fieldValue { get; set; }
        internal int recordId { get; set; } // used for related records in a portal
        internal bool hasRecordId { get; set; } // default value is false

		internal string fullName { get; private set; }

        internal string portalObjectName { get; private set; }

		internal KeyValuePair<string, string> namveValuePair { get; private set; }

        #region "constructors"        
        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="TO">The table occurance for the field.</param>
        /// <param name="repetition">The repetition number.</param>
        /// <param name="value">The field value.</param>
        /// <param name="recId">The internal FileMaker  record Id.</param>
        /// <param name="version">The version of FileMaker SErver</param>
        /// <param name="portalName">The name of the portal that contains this field</param>
        public Field(string name, string TO, int? repetition, string value, int? recId, int version, string portalName)
		{
			fieldName = name;
			tableOccurance = TO;
            portalObjectName = portalName;
            fmsVersion = version;

            if(repetition.HasValue)
			    repetitionNumber = repetition.Value;

            if(recId.HasValue)
            {
                recordId = recId.Value;
                hasRecordId = true;
            }

            fieldValue = value;

			fullName = composeFullName();
			namveValuePair = new KeyValuePair<string, string>(fullName, fieldValue);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        public Field(string name, int version) : this(name, "", 0, "", null, version, string.Empty)
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        public Field(string name, string value, int version) : this(name, "", 0, value, null, version, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="repetition">The repetition number.</param>
        public Field(string name, int repetition, int version) : this(name, "", repetition, "", null, version, string.Empty)
        { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="repetition">The repetition number.</param>
        /// <param name="value">The field value.</param>
        public Field(string name, int repetition, string value, int version) : this(name, "", repetition, value, null, version, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="TO">The table occurance for the field.</param>
        /// <param name="repetition">The repetition number.</param>
        /// <param name="value">The field value.</param>
        public Field(string name,string TO, int repetition, string value, int version) : this(name, TO, repetition, value, null, version, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        /// <param name="recId">The internal FileMaker  record Id.</param>
        public Field( string name, string value, int recId, int version) : this(name, "", 0, value, recId, version, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="TO">The table occurance for the field.</param>
        /// <param name="value">The field value.</param>
        /// <param name="recId">The internal FileMaker  record Id.</param>
        public Field(string name, string TO, string value, int recId, int version) : this(name, TO, 0, value, version, recId, string.Empty)
        {}



        #endregion

        private string composeFullName()
		{
			string fullyQualifiedName = fieldName;
			if (tableOccurance != null && tableOccurance.Length > 0)
			{
				fullyQualifiedName = tableOccurance + "::" + fieldName;
			}

			if (repetitionNumber > 1 )
			{
				fullyQualifiedName = fullyQualifiedName + "(" + repetitionNumber.ToString() + ")";
			}

			return fullyQualifiedName;
					
		}
	}
}
