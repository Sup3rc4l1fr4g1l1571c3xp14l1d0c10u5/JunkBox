using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GLPainter {
    public class Matrix3 {
        private float[] Value;

        public float[] ToArray() {
            return Value.ToArray();

        }

        public float this[int index] {
            get { return Value[index]; }
        }

        public Matrix3(params float[] p) {
            if (p.Length != 9) {
                throw new Exception();
            }

            Value = p;
        }

        public static Matrix3 projection(float width, float height) {
            // Note: This matrix flips the Y axis so that 0 is at the top.
            return new Matrix3(
                2 / width, 0, 0,
                0, 2 / height, 0,
                -1, -1, 1
            );
        }

        public static Matrix3 identity() {
            return new Matrix3(
                1, 0, 0,
                0, 1, 0,
                0, 0, 1
            );
        }

        public static Matrix3 translation(float tx, float ty) {
            return new Matrix3(
                1, 0, 0,
                0, 1, 0,
                tx, ty, 1
            );
        }

        public static Matrix3 rotation(float angleInRadians) {
            var c = (float) Math.Cos(angleInRadians);
            var s = (float) Math.Sin(angleInRadians);
            return new Matrix3(
                c, -s, 0,
                s, c, 0,
                0, 0, 1
            );
        }

        public static Matrix3 scaling(float sx, float sy) {
            return new Matrix3(
                sx, 0, 0,
                0, sy, 0,
                0, 0, 1
            );
        }

        public static Matrix3 multiply(Matrix3 a, Matrix3 b) {
            var a00 = a[0 * 3 + 0];
            var a01 = a[0 * 3 + 1];
            var a02 = a[0 * 3 + 2];
            var a10 = a[1 * 3 + 0];
            var a11 = a[1 * 3 + 1];
            var a12 = a[1 * 3 + 2];
            var a20 = a[2 * 3 + 0];
            var a21 = a[2 * 3 + 1];
            var a22 = a[2 * 3 + 2];
            var b00 = b[0 * 3 + 0];
            var b01 = b[0 * 3 + 1];
            var b02 = b[0 * 3 + 2];
            var b10 = b[1 * 3 + 0];
            var b11 = b[1 * 3 + 1];
            var b12 = b[1 * 3 + 2];
            var b20 = b[2 * 3 + 0];
            var b21 = b[2 * 3 + 1];
            var b22 = b[2 * 3 + 2];
            return new Matrix3(
                b00 * a00 + b01 * a10 + b02 * a20,
                b00 * a01 + b01 * a11 + b02 * a21,
                b00 * a02 + b01 * a12 + b02 * a22,
                b10 * a00 + b11 * a10 + b12 * a20,
                b10 * a01 + b11 * a11 + b12 * a21,
                b10 * a02 + b11 * a12 + b12 * a22,
                b20 * a00 + b21 * a10 + b22 * a20,
                b20 * a01 + b21 * a11 + b22 * a21,
                b20 * a02 + b21 * a12 + b22 * a22
            );
        }
    }

}