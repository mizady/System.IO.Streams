﻿//
// Copyright (c) .NET Foundation and Contributors
// Portions Copyright (c) Microsoft Corporation.  All rights reserved.
// See LICENSE file in the project root for full license information.
//

namespace System.IO
{
    /// <summary>
    /// Creates a stream whose backing store is memory.
    /// </summary>
    public class MemoryStream : Stream
    {
        // Either allocated internally or externally.
        private byte[] _buffer;
        // For user-provided arrays, start at this origin
        private int _origin;
        // read/write head.
        private int _position;
        // Number of bytes within the memory stream
        private int _length;
        // length of usable portion of buffer for stream
        private int _capacity;
        // User-provided buffers aren't expandable.
        private bool _expandable;
        // Is this stream open or closed?
        private bool _isOpen;

        private const int MemStreamMaxLength = 0xFFFF;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStream"/> class with an expandable capacity initialized to zero.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="CanRead"/>, <see cref="CanSeek"/>, and <see cref="CanWrite"/> properties are all set to <see langword="true"/>.
        /// </para>
        /// <para>
        /// The capacity of the current stream automatically increases when you use the <see cref="SetLength"/> method to set the length to a value larger than the capacity of the current stream.
        /// </para>
        /// </remarks>
        public MemoryStream()
        {
            _buffer = new byte[256];
            _capacity = 256;
            _expandable = true;

            // Must be 0 for byte[]'s created by MemoryStream
            _origin = 0;
            _isOpen = true;
        }

        /// <summary>
        /// Initializes a new non-resizable instance of the <see cref="MemoryStream"/> class based on the specified byte array.
        /// </summary>
        /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="CanRead"/>, <see cref="CanSeek"/>, and <see cref="CanWrite"/> properties are all set to <see langword="true"/>.
        /// </para>
        /// <para>
        /// The capacity of the current stream automatically increases when you use the <see cref="SetLength"/> method to set the length to a value larger than the capacity of the current stream.
        /// </para>
        /// </remarks>
        public MemoryStream(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException();

            _length = _capacity = buffer.Length;
            _expandable = false;
            _origin = 0;
            _isOpen = true;
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value>/// <see langword="true"/> if the stream is open./// </value>
        /// <remarks>
        /// <para>
        /// If a class derived from <see cref="Stream"/> does not support reading, calls to the <see cref="Read"/> and <see cref="ReadByte"/> methods throw a <see cref="NotSupportedException"/>.
        /// </para>
        /// <para>
        /// If the stream is closed, this property returns <see langword="false"/>.
        /// </para>
        /// </remarks>
        public override bool CanRead => _isOpen;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value><see langword="true"/> if the stream is open.</value>
        /// <remarks>
        /// <para>
        /// If a class derived from <see cref="Stream"/> does not support reading, calls to the <see cref="Length"/>, <see cref="SetLength"/>, <see cref="Position"/> and <see cref="Seek"/> throw a <see cref="NotSupportedException"/>.
        /// </para>
        /// <para>
        /// If the stream is closed, this property returns <see langword="false"/>.
        /// </para>
        /// </remarks>
        public override bool CanSeek => _isOpen;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value><see langword="true"/> if the stream supports writing; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        /// <para>
        /// If a class derived from <see cref="Stream"/> does not support reading, calls to the <see cref="SetLength(long)"/> and <see cref="Write"/> or <see cref="WriteByte"/> methods throw a <see cref="NotSupportedException"/>.
        /// </para>
        /// <para>
        /// If the stream is closed, this property returns <see langword="false"/>.
        /// </para>
        /// </remarks>
        public override bool CanWrite => _isOpen;

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isOpen = false;
            }
        }

        // returns a bool saying whether we allocated a new array.
        private bool EnsureCapacity(int value)
        {
            if (value > _capacity)
            {
                int newCapacity = value;

                if (newCapacity < 256)
                {
                    newCapacity = 256;
                }

                if (newCapacity < _capacity * 2)
                {
                    newCapacity = _capacity * 2;
                }

                if (!_expandable && newCapacity > _capacity)
                {
                    throw new NotSupportedException();
                }

                if (newCapacity > 0)
                {
                    byte[] newBuffer = new byte[newCapacity];

                    if (_length > 0)
                    {
                        Array.Copy(_buffer, 0, newBuffer, 0, _length);
                    }

                    _buffer = newBuffer;
                }
                else
                {
                    _buffer = null;
                }

                _capacity = newCapacity;

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                if (!_isOpen)
                {
                    throw new ObjectDisposedException();
                }

                return _length - _origin;
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                if (!_isOpen)
                {
                    throw new ObjectDisposedException();
                }

                return _position - _origin;
            }

            set
            {
                if (!_isOpen)
                {
                    throw new ObjectDisposedException();
                }

                if (value < 0 || value > MemStreamMaxLength)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _position = _origin + (int)value;
            }
        }


        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The current stream instance is closed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> subtracted from the buffer length is less than <paramref name="count"/>.</exception>
        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException();
            }

            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentException();
            }

            int n = _length - _position;

            if (n > count)
            {
                n = count;
            }

            if (n <= 0)
            {
                return 0;
            }

            Array.Copy(_buffer, _position, buffer, offset, n);
            _position += n;

            return n;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The current stream instance is closed.</exception>
        public override int ReadByte()
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException();
            }

            if (_position >= _length)
            {
                return -1;
            }

            return _buffer[_position++];
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is greater than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="IOException">Seeking is attempted before the beginning of the stream.</exception>
        /// <exception cref="ArgumentException">
        /// There is an invalid <see cref="SeekOrigin"/>.
        /// -or-
        /// <paramref name="offset"/> caused an arithmetic overflow.
        /// </exception>
        public override long Seek(
            long offset,
            SeekOrigin origin)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException();
            }

            if (offset > MemStreamMaxLength)
            {
                throw new ArgumentOutOfRangeException();
            }

            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                    {
                        throw new IOException();
                    }

                    _position = _origin + (int)offset;
                    break;

                case SeekOrigin.Current:
                    if (offset + _position < _origin)
                    {
                        throw new IOException();
                    }

                    _position += (int)offset;
                    break;

                case SeekOrigin.End:
                    if (_length + offset < _origin)
                    {
                        throw new IOException();
                    }

                    _position = _length + (int)offset;
                    break;

                default:
                    throw new ArgumentException();
            }

            return _position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException();
            }

            if (value > MemStreamMaxLength || value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int newLength = _origin + (int)value;
            bool allocatedNewArray = EnsureCapacity(newLength);

            if (!allocatedNewArray && newLength > _length)
            {
                Array.Clear(_buffer, _length, newLength - _length);
            }

            _length = newLength;

            if (_position > newLength)
            {
                _position = newLength;
            }
        }

        /// <inheritdoc/>
        public virtual byte[] ToArray()
        {
            byte[] copy = new byte[_length - _origin];

            Array.Copy(_buffer, _origin, copy, 0, _length - _origin);

            return copy;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException();
            }

            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentException();
            }

            int i = _position + count;

            // check for overflow
            if (i > _length)
            {
                if (i > _capacity)
                {
                    EnsureCapacity(i);
                }

                _length = i;
            }

            Array.Copy(buffer, offset, _buffer, _position, count);
            _position = i;
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException();
            }

            if (_position >= _capacity)
            {
                EnsureCapacity(_position + 1);
            }

            _buffer[_position++] = value;

            if (_position > _length)
            {
                _length = _position;
            }
        }

        /// <inheritdoc/>
        public virtual void WriteTo(Stream stream)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException();
            }

            if (stream == null)
            {
                throw new ArgumentNullException();
            }

            stream.Write(_buffer, _origin, _length - _origin);
        }
    }
}