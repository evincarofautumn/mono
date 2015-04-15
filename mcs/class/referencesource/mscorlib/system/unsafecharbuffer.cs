// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  UnSafeBuffer
**
** Purpose: A class to detect incorrect usage of UnSafeBuffer
**
** 
===========================================================*/

namespace System {
    using System.Security;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    
    unsafe internal struct UnSafeCharBuffer{
        [SecurityCritical]
        byte * m_buffer;
        int m_totalSize;
        int m_length;    
        bool m_isCompact;
    
        [System.Security.SecurityCritical]  // auto-generated
        public UnSafeCharBuffer( byte *buffer,  int bufferSize, bool compact) {
            Contract.Assert( buffer != null, "buffer pointer can't be null."  );
            Contract.Assert( bufferSize >= 0, "buffer size can't be negative."  );        
            m_buffer = buffer;
            m_totalSize = bufferSize;    
            m_length = 0;
            m_isCompact = compact;
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AppendString(string stringToAppend) {
            if( String.IsNullOrEmpty( stringToAppend ) ) {
                return;
            }
            
            if ( (m_totalSize - m_length) < stringToAppend.Length ) {
                throw new IndexOutOfRangeException();
            }
            
#if MONO
            fixed( byte* pointerToString = &stringToAppend.m_firstByte ) {
                if (stringToAppend.IsCompact) {
                    if (m_isCompact) {
                        Buffer.Memcpy((byte*)(m_buffer + m_length), pointerToString, stringToAppend.Length * sizeof(byte));
                    } else {
                        for (int i = 0; i < stringToAppend.Length; ++i)
                            ((char*)m_buffer)[i + m_length] = (char)pointerToString [i];
                    }
                } else {
                    Contract.Assert(!m_isCompact, "Cannot fill compact buffer from non-compact strings.");
                    Buffer.Memcpy( (byte*) (m_buffer + m_length * sizeof(char)), pointerToString, stringToAppend.Length * sizeof(char));
                }
            }
#else
            fixed( char* pointerToString = stringToAppend ) {
                Contract.Assert(!m_isCompact, "Cannot fill compact buffer from non-compact strings.");
                Buffer.Memcpy( (byte*) (m_buffer + m_length * sizeof(char)), (byte *) pointerToString, stringToAppend.Length * sizeof(char));
            }
#endif
            m_length += stringToAppend.Length;
            Contract.Assert(m_length <= m_totalSize, "Buffer has been overflowed!");
        }
                
        public int Length {
            get {
                return m_length;
            } 
        }   
    }    
} 
