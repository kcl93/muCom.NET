using System;
using System.Reflection;

namespace MuCom
{
    internal class LinkedVariable
    {
        #region Fields

        internal Type type;
        internal int byteCount;
        internal object target;
        internal object info;

        #endregion

        #region Constructor

        internal LinkedVariable(object info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            if (((info is PropertyInfo) == false) && ((info is FieldInfo) == false))
            {
                throw new ArgumentException("Info object is neither PropertyInfo nor FieldInfo!");
            }
            this.info = info;
        }

        #endregion

        #region Methods

        internal void Write(object value)
        {
            if (this.info is null) throw new ArgumentNullException(nameof(this.info));
            if (this.info is PropertyInfo)
            {
                (this.info as PropertyInfo).SetValue(this.target, value);
            }
            else if (this.info is FieldInfo)
            {
                (this.info as FieldInfo).SetValue(this.target, value);
            }
        }

        internal object Read()
        {
            if (this.info is null) throw new ArgumentNullException(nameof(this.info));
            if (this.info is PropertyInfo)
            {
                return (this.info as PropertyInfo).GetValue(this.target);
            }
            else if (this.info is FieldInfo)
            {
                return (this.info as FieldInfo).GetValue(this.target);
            }
            return null;
        }

        #endregion
    }
}
