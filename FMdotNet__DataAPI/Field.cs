using System;
using System.Collections.Generic;

namespace FMdotNet__DataAPI
{
    /// <summary>
    /// Represents a FileMaker field and its attributes
    /// </summary>
    public class Field
	{

		internal string fieldName { get; set; }
		internal string tableOccurance { get; set; }
		internal int repetitionNumber { get; set; } // nullable
		internal string fieldValue { get; set; }
        internal int recordId { get; set; } // used for related records in a portal
        internal bool hasRecordId { get; set; } // default value is false

		internal string fullName { get; private set; }

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
        public Field(string name, string TO, int? repetition, string value, int? recId)
		{
			fieldName = name;
			tableOccurance = TO;

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
        public Field(string name) : this(name, "", 0, "", null)
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        public Field(string name, string value) : this(name, "", 0, value, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="repetition">The repetition number.</param>
        public Field(string name, int repetition) : this(name, "", repetition, "", null)
        { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="repetition">The repetition number.</param>
        /// <param name="value">The field value.</param>
        public Field(string name, int repetition, string value) : this(name, "", repetition, value, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="TO">The table occurance for the field.</param>
        /// <param name="repetition">The repetition number.</param>
        /// <param name="value">The field value.</param>
        public Field(string name,string TO, int repetition, string value) : this(name, TO, repetition, value, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The field value.</param>
        /// <param name="recId">The internal FileMaker  record Id.</param>
        public Field( string name, string value, int recId) : this(name, "", 0, value, recId)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Field"/> class.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="TO">The table occurance for the field.</param>
        /// <param name="value">The field value.</param>
        /// <param name="recId">The internal FileMaker  record Id.</param>
        public Field(string name, string TO, string value, int recId) : this(name, TO, 0, value, recId)
        {}



        #endregion

        private string composeFullName()
		{
			string fullyQualifiedName = fieldName;
			if (tableOccurance != null && tableOccurance.Length > 0)
			{
				fullyQualifiedName = tableOccurance + "::" + fieldName;
			}

			if (repetitionNumber > 0 )
			{
				fullyQualifiedName = fullyQualifiedName + "(" + repetitionNumber.ToString() + ")";
			}
            if(hasRecordId == true)
            {
                fullyQualifiedName = fullyQualifiedName + "." + recordId.ToString();
            }

			return fullyQualifiedName;
					
		}
	}
}
