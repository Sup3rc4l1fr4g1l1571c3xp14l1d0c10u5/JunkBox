#define _USE_MATH_DEFINES

#include <iostream>
#include <vector>
#include <algorithm>
#include <strstream>
#include <complex>

#include <cstdio>
#include <cstdint>
#include <cstdlib>
#include <cmath>

template<typename T>
class Matrix {
private:
	size_t _row, _col;
	std::vector<T> _values;

	size_t row() const { return this->_row; }
	size_t col() const { return this->_col; }

	Matrix(size_t row, size_t col) :
		_row(row),
		_col(col),
		_values(row*col)
	{}
	Matrix(const size_t row, const size_t col, const std::vector<T> &values) :
		_row(row),
		_col(col),
		_values(values)
	{
		if (row < 0 || col < 0 || values.size() != row * col) {
			throw std::exception("out of index");
		}
	}
	Matrix(const size_t row, const size_t col, const std::vector<T> &&values) :
		_row(row),
		_col(col),
		_values(values)
	{
		if (row < 0 || col < 0 || values.size() != row * col) {
			throw std::exception("out of index");
		}
	}
public:
	Matrix() :
		_row(0),
		_col(0),
		_values()
	{ }
	Matrix(const Matrix &m) :
		_row(m._row),
		_col(m._col),
		_values(m._values)
	{}
	Matrix(Matrix &&m) {
		std::swap(this->_row, m._row);
		std::swap(this->_col, m._col);
		std::swap(this->_values, m._values);
	}
	Matrix &operator=(const Matrix &m) {
		this->_row = m._row;
		this->_col = m._col;
		this->_values.resize(m._values.size());
		std::copy(m._values.begin(), m._values.end(), this->_values.begin());
		return *this;
	}
	Matrix &operator=(Matrix &&m) {
		std::swap(this->_row, m._row);
		std::swap(this->_col, m._col);
		std::swap(this->_values, m._values);
		return *this;
	}
	static Matrix create(size_t row, size_t col, const T& value) {
		std::vector<T> ret(row * col);
		std::fill(ret.begin(), ret.end(), value);
		return Matrix(row, col, ret);
	}
	static Matrix create(size_t row, size_t col, T(*gen)(int32_t, int32_t)) {
		std::vector<T> ret(row * col);
		auto n = 0;
		for (size_t i = 0; i < row; i++) {
			for (size_t j = 0; j < col; j++) {
				ret[n++] = gen(i, j);
			}
		}
		return Matrix(row, col, ret);
	}
	static Matrix fromVector(const size_t &row, const size_t &col, const std::vector<T> &values) {
		return Matrix(row, col, values);
	}
	static Matrix fromVector(const size_t &row, const size_t &col, const std::vector<T> &&values) {
		return Matrix(row, col, values);
	}
	static Matrix fromArray(const size_t &row, const size_t &col, const T *values, const size_t length) {
		return Matrix(row, col, std::vector<T>(values, values + length));
	}

	Matrix clone() const {
		return Matrix(*this);
	}

	std::vector<T> toVector() const {
		return std::vector<T>(this->_values);
	}

	std::string toString() const {
		std::strstream imploded;
		imploded << "[";
		for (size_t j = 0; j < this->_row; j++) {
			imploded << ((j != 0) ? ", " : "");
			imploded << "[";
			for (size_t i = 0; i < this->_col; i++) {
				imploded << ((i != 0) ? ", " : "");
				imploded << this->_values[this->at(j, i)];
			}
			imploded << "]";
		}
		imploded << "]";
		imploded << std::ends;
		return std::string(imploded.str());
	}

	const int32_t at(const size_t &row, const size_t &col) const {
		if (row < 0 || this->_row <= row || col < 0 || this->_col <= col) {
			throw std::exception("out of index");
		}
		return this->_col * row + col;
	}

	const std::vector<T> getRow(const size_t &row) const {
		if (row < 0 || this->_row <= row) {
			throw std::exception("out of index");
		}
		std::vector<T> ret;
		for (size_t j = 0; j < this->_col; j++) {
			ret.push_back(this->_values[this->at(row, j)]);
		}
		return ret;
	}

	void setRow(const size_t row, const std::vector<T> &values) {
		if (row < 0 || this->_row <= row) {
			throw std::exception("out of index");
		}
		if (this->_col != values.size()) {
			throw std::exception("column length mismatch");
		}
		for (size_t j = 0; j < this->_col; j++) {
			this->_values[this->at(row, j)] = values[j];
		}
	}

	std::vector<T> getCol(const size_t col) const {
		if (col < 0 || this->_col <= col) {
			throw std::exception("out of index");
		}
		std::vector<T> ret;
		for (size_t j = 0; j < this->_row; j++) {
			ret.push_back(this->_values[this->at(j, col)]);
		}
		return ret;
	}

	void setCol(const size_t col, const std::vector<T> &values) {
		if (col < 0 || this->_col <= col) {
			throw std::exception("out of index");
		}
		if (this->_row != values.size()) {
			throw std::exception("row length mismatch");
		}
		for (size_t j = 0; j < this->_row; j++) {
			this->_values[this->at(j, col)] = values[j];
		}
	}

	template<class TF>
	Matrix<T> map(const TF &func) const {
		Matrix<T> ret(this->_row, this->_col);
		std::transform(this->_values.begin(), this->_values.end(), ret._values.begin(), func);
		return ret;
	}

	static bool equal(const Matrix<T> &m1, const Matrix<T> &m2) {
		return std::equal(m1._values.begin(), m1._values.end(), m2._values.begin(), m2._values.end());
	}

	static Matrix<T> add(const Matrix<T> &m1, const Matrix<T> &m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		Matrix<T> ret(m1._row, m1._col);
		for (size_t i = 0; i < size; i++) {
			ret._values[i] = m1._values[i] + m2._values[i];
		}
		return ret;
	}

	static Matrix<T> &add(const Matrix<T> &&m1, const Matrix<T> &m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		for (size_t i = 0; i < size; i++) {
			m1._values[i] += m2._values[i];
		}
		return m1;
	}

	static Matrix<T> &add(const Matrix<T> &m1, Matrix<T> &&m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		for (size_t i = 0; i < size; i++) {
			m2._values[i] += m1._values[i];
		}
		return m2;
	}

	static Matrix<T> sub(const Matrix<T> &m1, const Matrix<T> &m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		Matrix<T> ret(m1._row, m1._col);
		for (size_t i = 0; i < size; i++) {
			ret._values[i] = m1._values[i] - m2._values[i];
		}
		return ret;
	}
	static Matrix<T> &sub(Matrix<T> &&m1, const Matrix<T> &m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		for (size_t i = 0; i < size; i++) {
			m1._values[i] -= m2._values[i];
		}
		return m1;
	}
	static Matrix<T> &sub(Matrix<T> &m1, const Matrix<T> &&m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		for (size_t i = 0; i < size; i++) {
			m2._values[i] = m1._values[i] - m2._values[i];
		}
		return m2;
	}

	static Matrix<T> scalarMultiplication(const Matrix<T> &m, const T &s) {
		return m.map([=](const T& x) -> const T { return x * s; });
	}
	static Matrix<T> &scalarMultiplication(Matrix<T> &&m, const T &s) {
		for (size_t i = 0; i < m._values.size(); i++) {
			m._values[i] *= s;
		}
		return m;
	}

	static Matrix<T> dotProduct(const Matrix<T> &m1, const Matrix<T> &m2) {
		if (m1._col != m2._row) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m2._col;
		Matrix<T> ret(m1._row, m2._col);
		auto n = 0;
		const auto dp = 1;
		const auto dq = m2._col;
		for (size_t i = 0; i < m1._row; i++) {
			for (size_t j = 0; j < m2._col; j++) {
				T sum = T();
				auto p = m1.at(i, 0);
				auto q = m2.at(0, j);
				for (size_t k = 0; k < m1._col; k++) {
					sum += m1._values[p] * m2._values[q];
					p += dp;
					q += dq;
				}
				ret._values[n++] = sum;
			}
		}
		return ret;
	}

	static Matrix<T> hadamardProduct(const Matrix<T> &m1, const Matrix<T> &m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		Matrix<T> ret(m1._row, m1._col);
		for (size_t i = 0; i < size; i++) {
			ret._values[i] = m1._values[i] * m2._values[i];
		}
		return ret;
	}
	static Matrix<T> &hadamardProduct(Matrix<T> &&m1, const Matrix<T> &m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		for (size_t i = 0; i < size; i++) {
			m1._values[i] *= m2._values[i];
		}
		return m1;
	}
	static Matrix<T> &hadamardProduct(const Matrix<T> &m1, Matrix<T> &&m2) {
		if (m1._row != m2._row || m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m1._col;
		for (size_t i = 0; i < size; i++) {
			m2._values[i] *= m1._values[i];
		}
		return m2;
	}

	static Matrix<T> transpose(const Matrix<T> &m) {
		const auto size = m._values.size();
		Matrix<T> ret(m._col, m._row);
		auto p = 0;
		const auto dq = m._col;
		for (size_t j = 0; j < m._col; j++) {
			auto q = m.at(0, j);
			for (size_t i = 0; i < m._row; i++) {
				ret._values[p] = m._values[q];
				p += 1;
				q += dq;
			}
		}
		return ret;
	}

	// HxI * transpose(JxI) = HxJ
	static Matrix<T> dotProductWithTransposeLeft(const Matrix<T> &m1, const Matrix<T> &m2) {
		if (m1._col != m2._col) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._row * m2._row;
		Matrix<T> ret(m1._row, m2._row);
		auto n = 0;
		const auto dp = 1;
		const auto dq = 1;
		for (size_t i = 0; i < m1._row; i++) {
			for (size_t j = 0; j < m2._row; j++) {
				T sum = T();
				auto p = m1.at(i, 0);
				auto q = m2.at(j, 0);
				for (size_t k = 0; k < m1._col; k++) {
					sum += m1._values[p] * m2._values[q];
					p += dp;
					q += dq;
				}
				ret._values[n++] = sum;
			}
		}
		return ret;
	}

	// transpose(IxH) * IxJ = HxJ
	static Matrix<T> dotProductWithTransposeRight(const Matrix<T> &m1, const Matrix<T> &m2) {
		if (m1._row != m2._row) {
			throw std::exception("dimension mismatch");
		}
		const auto size = m1._col * m2._col;
		Matrix<T> ret(m1._col, m2._col);
		auto n = 0;
		const auto dp = m1._col;
		const auto dq = m2._col;
		for (size_t i = 0; i < m1._col; i++) {
			for (size_t j = 0; j < m2._col; j++) {
				T sum = T();
				auto p = m1.at(0, i);
				auto q = m2.at(0, j);
				for (size_t k = 0; k < m1._row; k++) {
					sum += m1._values[p] * m2._values[q];
					p += dp;
					q += dq;
				}
				ret._values[n++] = sum;
			}
		}
		return ret;
	}

	static bool test() {
		auto arrayEqual = [](const std::vector<int> &v1, const std::vector<int> &v2) { return std::equal(v1.begin(), v1.end(), v2.begin(), v2.end()); };
		// hadamardProduct test
		{
			auto m1 = Matrix<int>::fromVector(3, 2, std::vector<int> { 2, 4, 6, 8, 10, 12 });
			auto m2 = Matrix<int>::fromVector(3, 2, std::vector<int> { 3, 5, 7, 9, 11, 13 });
			auto m3 = Matrix<int>::hadamardProduct(m1, m2);
			auto m4 = Matrix<int>::fromVector(3, 2, std::vector<int> { 6, 20, 42, 72, 110, 156 });
			if (!Matrix<int>::equal(m3, m4)) { return false; }
			std::cout << "hadamardProduct: ok" << std::endl;
		}

		// scalarMultiplication test
		{
			std::vector<int> v1 = { 2, 4, 6, 8, 10, 12 };
			std::vector<int> v2(v1.size());
			std::transform(v1.begin(), v1.end(), v2.begin(), [](const auto &n) { return n * -4; });
			auto m1 = Matrix<int>::fromVector(3, 2, v1);
			auto m2 = Matrix<int>::scalarMultiplication(m1, -4);
			auto m3 = Matrix<int>::fromVector(3, 2, v2);
			if (!Matrix<int>::equal(m2, m3)) { return false; }
			std::cout << "scalarMultiplication: ok" << std::endl;
		}

		// dotProduct test
		{
			auto m1 = Matrix<int>::fromVector(3, 2, std::vector<int>{ 2, 4, 6, 8, 10, 12 });
			auto m2 = Matrix<int>::fromVector(2, 3, std::vector<int>{ 3, 5, 7, 13, 11, 9 });
			auto m3 = Matrix<int>::dotProduct(m1, m2);
			auto m4 = Matrix<int>::fromVector(3, 3, std::vector<int> { 58, 54, 50, 122, 118, 114, 186, 182, 178 });
			if (!Matrix<int>::equal(m3, m4)) { return false; }
			std::cout << "dotProduct: ok" << std::endl;
		}

		// transpose test
		{
			auto m1 = Matrix<int>::fromVector(3, 2, std::vector<int> { 2, 4, 6, 8, 10, 12 });
			auto m2 = Matrix<int>::transpose(m1);
			auto m3 = Matrix<int>::fromVector(2, 3, std::vector<int> { 2, 6, 10, 4, 8, 12 });
			if (!Matrix<int>::equal(m2, m3)) { return false; }
			std::cout << "transpose: ok" << std::endl;
		}
		// dotProductWithTransposeLeft test
		{
			auto m1 = Matrix<int>::fromVector(3, 2, std::vector<int>{ 2, 4, 6, 8, 10, 12 });
			auto m2 = Matrix<int>::fromVector(2, 3, std::vector<int>{ 3, 5, 7, 13, 11, 9 });
			auto m3 = Matrix<int>::transpose(m2);
			auto m4 = Matrix<int>::dotProductWithTransposeLeft(m1, m3);
			auto m5 = Matrix<int>::fromVector(3, 3, std::vector<int> { 58, 54, 50, 122, 118, 114, 186, 182, 178 });
			if (!Matrix<int>::equal(m4, m5)) { return false; }
			std::cout << "dotProductWithTransposeLeft: ok" << std::endl;

		}
		// dotProductWithTransposeRight test
		{
			auto m1 = Matrix<int>::fromVector(3, 2, std::vector<int>{ 2, 4, 6, 8, 10, 12 });
			auto m2 = Matrix<int>::fromVector(2, 3, std::vector<int>{ 3, 5, 7, 13, 11, 9 });
			auto m3 = Matrix<int>::transpose(m1);
			auto m4 = Matrix<int>::dotProductWithTransposeRight(m3, m2);
			auto m5 = Matrix<int>::fromVector(3, 3, std::vector<int> { 58, 54, 50, 122, 118, 114, 186, 182, 178 });
			if (!Matrix<int>::equal(m4, m5)) { return false; }
			std::cout << "dotProductWithTransposeRight: ok" << std::endl;

		}

		// toString test
		{
			auto s1 = Matrix<int>::fromVector(3, 2, std::vector<int> { 2, 4, 6, 8, 10, 12 }).toString();
			auto s2 = std::string("[[2, 4], [6, 8], [10, 12]]");
			if (s1 != s2) { return false; }
			std::cout << "toString: ok" << std::endl;
		}

		// getRow test
		{
			auto  m1 = Matrix<int>::fromVector(2, 3, std::vector<int> { 3, 5, 7, 13, 11, 9 });
			auto r0 = m1.getRow(0);
			auto r1 = m1.getRow(1);
			if (!arrayEqual(r0, std::vector<int>{ 3, 5, 7 })) { return false; }
			if (!arrayEqual(r1, std::vector<int>{13, 11, 9})) { return false; }
			std::cout << "getRow: ok" << std::endl;
		}

		// getCol test
		{
			auto m1 = Matrix<int>::fromVector(2, 3, std::vector<int> { 3, 5, 7, 13, 11, 9 });
			auto c0 = m1.getCol(0);
			auto c1 = m1.getCol(1);
			auto c2 = m1.getCol(2);
			if (!arrayEqual(c0, std::vector < int>{3, 13})) { return false; }
			if (!arrayEqual(c1, std::vector < int>{5, 11})) { return false; }
			if (!arrayEqual(c2, std::vector < int>{7, 9})) { return false; }
			std::cout << "getCol: ok" << std::endl;
		}

		// setRow test
		{
			auto m1 = Matrix<int>::fromVector(2, 3, std::vector < int>{1, 2, 3, 4, 5, 6});
			m1.setRow(0, std::vector<int>{7, 8, 9});
			m1.setRow(1, std::vector<int>{10, 11, 12});
			if (!arrayEqual(m1.toVector(), std::vector<int>{7, 8, 9, 10, 11, 12})) { return false; }
			std::cout << "setRow: ok" << std::endl;
		}

		// setCol test
		{
			auto m1 = Matrix<int>::fromVector(2, 3, std::vector<int>{1, 2, 3, 4, 5, 6});
			m1.setCol(0, std::vector<int>{7, 8});
			m1.setCol(1, std::vector<int>{9, 10});
			m1.setCol(2, std::vector<int>{11, 12});
			if (!arrayEqual(m1.toVector(), std::vector < int>{7, 9, 11, 8, 10, 12})) { return false; }
			std::cout << "setCol: ok" << std::endl;
		}

		std::cout << "overall: ok" << std::endl;
		return true;
	}
};

class MNistImage {
private:
	const int32_t _id;
	const int32_t _width;
	const int32_t _height;
	const std::vector<uint8_t> _pixels;
public:
	int32_t id() const { return this->_id; }
	int32_t width() const { return this->_width; }
	int32_t height() const { return this->_width; }
	const uint8_t *pixels() const { return this->_pixels.data(); }
	std::vector<double> toNormalizedVector() {
		std::vector<double> nvec(this->_pixels.size());
		std::transform(_pixels.begin(), _pixels.end(), nvec.begin(), [](auto x) { return x / 255.0 * 0.99 + 0.01; });
		return nvec;
	}

	MNistImage()
		: _id(-1)
		, _width(-1)
		, _height(-1)
		, _pixels()
	{ }
	MNistImage(int32_t id, int32_t width, int32_t height, const std::vector<uint8_t> &pixels)
		: _id(id)
		, _width(width)
		, _height(height)
		, _pixels(pixels)
	{ }
	MNistImage(const MNistImage &other)
		: _id(other._id)
		, _width(other._width)
		, _height(other._height)
		, _pixels(other._pixels)
	{ }
};
class MNistLabel {
private:
	const int32_t _id;
	const int32_t _label;
public:
	int32_t id() const { return this->_id; }
	int32_t label() const { return this->_label; }
	MNistLabel(int32_t id, int32_t label)
		: _id(id)
		, _label(label)
	{ }
	MNistLabel()
		: _id(-1)
		, _label(-1)
	{ }
	MNistLabel(const MNistLabel &other)
		: _id(other._id)
		, _label(other._label)
	{ }
	std::vector<double> toNormalizedVector() {
		std::vector<double> nvec(10, 0.01);
		nvec[this->_label] = 0.99;
		return nvec;
	}
};

int32_t big2little(int32_t big) {
	union {
		int32_t value;
		uint8_t bytes[4];
	} from, to;
	from.value = big;
	for (size_t i = 0; i < 4; i++) {
		to.bytes[i] = from.bytes[3 - i];
	};
	return to.value;
}
uint32_t big2little(uint32_t big) {
	union {
		uint32_t value;
		uint8_t bytes[4];
	} from, to;
	from.value = big;
	for (size_t i = 0; i < 4; i++) {
		to.bytes[i] = from.bytes[3 - i];
	};
	return to.value;
}

bool loadMNistImage(std::vector<MNistImage> &images, const char *path) {
	do {
		FILE *fp;
		{
			if (fopen_s(&fp, path, "rb") || fp == nullptr) {
				goto failed1;
			}
			uint32_t fourcc;
			if (fread_s(&fourcc, sizeof(fourcc), sizeof(fourcc), 1, fp) != 1) {
				goto failed1;
			}
			fourcc = big2little(fourcc);
			if (fourcc != 0x00000803U) {
				goto failed1;
			}
			int32_t count, width, height;
			if (fread_s(&count, sizeof(count), sizeof(count), 1, fp) != 1) {
				goto failed1;
			}
			if (fread_s(&width, sizeof(width), sizeof(width), 1, fp) != 1) {
				goto failed1;
			}
			if (fread_s(&height, sizeof(height), sizeof(height), 1, fp) != 1) {
				goto failed1;
			}
			count = big2little(count);
			width = big2little(width);
			height = big2little(height);
			auto size = width * height;
			std::vector<uint8_t> pixels(size);
			for (auto i = 0; i < count; i++) {
				if (fread_s(pixels.data(), pixels.size(), pixels.size(), 1, fp) != 1) {
					goto failed1;
				}
				images.push_back(MNistImage(i, width, height, pixels));
			}
			if (fp) { fclose(fp); }
			return true;
		}
	failed1:
		if (fp) { fclose(fp); }
		return false;

	} while (false);

}

bool loadMNistLabel(std::vector<MNistLabel> &labels, const char *path) {
	do {
		FILE *fp;
		{
			if (fopen_s(&fp, path, "rb") || fp == nullptr) {
				goto failed2;
			}
			uint32_t fourcc;
			if (fread_s(&fourcc, sizeof(fourcc), sizeof(fourcc), 1, fp) != 1) {
				goto failed2;
			}
			fourcc = big2little(fourcc);
			if (fourcc != 0x00000801U) {
				goto failed2;
			}

			int32_t count;
			if (fread_s(&count, sizeof(count), sizeof(count), 1, fp) != 1) {
				goto failed2;
			}
			count = big2little(count);
			for (auto i = 0; i < count; i++) {
				uint8_t label;
				if (fread_s(&label, sizeof(label), sizeof(label), 1, fp) != 1) {
					goto failed2;
				}
				labels.push_back(MNistLabel(i, label));
			}
			if (fp) { fclose(fp); }
			return true;
		}
	failed2:
		if (fp) { fclose(fp); }
		return false;
	} while (false);
}

double normRand() {
	auto r1 = (rand() * 1.0) / ((double)RAND_MAX + 1.0);
	auto r2 = (rand() * 1.0) / ((double)RAND_MAX + 1.0);
	return (sqrt(-2.0 * log(r1)) * cos(2.0 * M_PI * r2)) * 0.1;
}


template<typename T>
struct ActivationFunction {
	void(*const activate)(T *dest, const T *src, size_t len);
	void(*const derivate)(T *dest, const T *src, size_t len);
	ActivationFunction() : activate(nullptr), derivate(nullptr) {}
	ActivationFunction(
		void(*_activate)(T *dest, const T *src, size_t len),
		void(*_derivate)(T *dest, const T *src, size_t len)
	) : activate(_activate), derivate(_derivate) {}
	ActivationFunction(const ActivationFunction &other) : activate(other.activate), derivate(other.derivate) {}
};

const ActivationFunction<double> SigmoidFunction(
	[](auto *dest, const auto *src, size_t len) {
	for (size_t i = 0; i < len; i++) {
		dest[i] = 1.0 / (1.0 + exp(-src[i]));
	}
},
[](auto *dest, const auto *src, size_t len) {
	for (size_t i = 0; i < len; i++) {
		dest[i] = (1.0 - src[i]) * src[i];
	}
}
);

class NeuralNetwork {
private:
	std::vector<size_t> _layers;
	std::vector<Matrix<double>> _outputs;
	std::vector<Matrix<double>> _errors;
	std::vector<Matrix<double>> _weights;
	ActivationFunction<double>  _activationFunction;
public:
	std::vector<double> output() const { return this->_outputs[this->_outputs.size() - 1].getRow(0); }
	int32_t maxOutputIndex()  const {
		auto output = this->output();
		auto it = std::max_element(output.begin(), output.end(), [](const auto &x, const auto &y) { return (x < y); });
		if (it == output.end()) { return -1; }
		else { return std::distance(output.begin(), it); }
	}


	NeuralNetwork(std::initializer_list<size_t> layers)
		: _layers(layers)
		, _outputs(std::vector<Matrix<double>>(layers.size()))
		, _errors(std::vector<Matrix<double>>(layers.size()))
		, _weights(std::vector<Matrix<double>>(layers.size() - 1))
		, _activationFunction(SigmoidFunction)
	{
		std::transform(layers.begin(), layers.end(), this->_outputs.begin(), [](const auto &x) { return Matrix<double>::create(1, x, 0.0); });
		std::transform(layers.begin(), layers.end(), this->_errors.begin(), [](const auto &x) { return Matrix<double>::create(1, x, 0.0); });
		for (size_t i = 0; i < layers.size() - 1; i++) {
			_weights[i] = Matrix<double>::create(this->_layers[i + 0], this->_layers[i + 1], [](auto x, auto y) { _CRT_UNUSED(x); _CRT_UNUSED(y); return normRand(); });
		}
	}

	NeuralNetwork *prediction(const std::vector<double> &data) {
		if (data.size() != this->_layers[0]) {
			throw std::exception("length mismatch");
		}
		this->_outputs[0].setRow(0, data);
		for (size_t i = 1; i < this->_layers.size(); i++) {
			const auto src = Matrix<double>::dotProduct(this->_outputs[i - 1], this->_weights[i - 1]).getRow(0);
			const auto dst = std::vector<double>(src.size());
			this->_activationFunction.activate((double*)dst.data(), src.data(), src.size());
			this->_outputs[i].setRow(0, dst);
		}
		return this;
	}

	NeuralNetwork *train(const std::vector<double> &data, const std::vector<double> &label, double alpha) {
		if (data.size() != this->_layers[0]) {
			throw std::exception("length mismatch");
		}
		if (label.size() != this->_layers[this->_layers.size() - 1]) {
			throw std::exception("length mismatch");
		}

		this->prediction(data);

		auto labelMatrix = Matrix<double>::fromVector(1, label.size(), label);
		this->_errors[this->_outputs.size() - 1] = Matrix<double>::sub(labelMatrix, this->_outputs[this->_outputs.size() - 1]);
		for (int i = (int)this->_outputs.size() - 2; i >= 0; i--) {
			//this->_errors[i] = Matrix<double>::dotProduct(this->_errors[i + 1], Matrix<double>::transpose(this->_weights[i]));
			this->_errors[i] = Matrix<double>::dotProductWithTransposeLeft(this->_errors[i + 1], this->_weights[i]);
		}

		for (int i = 0; i < (int)this->_layers.size() - 1; i++) {
			const auto src = this->_outputs[i + 1].getRow(0);
			const auto dst = std::vector<double>(src.size());
			this->_activationFunction.derivate((double*)dst.data(), src.data(), src.size());
			const auto m = Matrix<double>::fromVector(1, src.size(), dst);
			//this->_weights[i] = Matrix<double>::add(this->_weights[i], Matrix<double>::scalarMultiplication(Matrix<double>::dotProduct(Matrix<double>::transpose(this->_outputs[i]), Matrix<double>::hadamardProduct(m, this->_errors[i + 1])), alpha));
			this->_weights[i] = Matrix<double>::add(this->_weights[i], Matrix<double>::scalarMultiplication(Matrix<double>::dotProductWithTransposeRight(this->_outputs[i], Matrix<double>::hadamardProduct(m, this->_errors[i + 1])), alpha));
		}
		return this;
	}

};

int main(void) {
	if (!Matrix<int>::test()) {
		return -1;
	}

	std::vector<MNistImage> trainImages;
	if (loadMNistImage(trainImages, "train-images.idx3-ubyte") == false) {
		return -1;
	}

	std::vector<MNistLabel> trainLabels;
	if (loadMNistLabel(trainLabels, "train-labels.idx1-ubyte") == false) {
		return -1;
	}

	std::vector<MNistImage> testImages;
	if (loadMNistImage(testImages, "t10k-images.idx3-ubyte") == false) {
		return -1;
	}

	std::vector<MNistLabel> testLabels;
	if (loadMNistLabel(testLabels, "t10k-labels.idx1-ubyte") == false) {
		return -1;
	}

	{
		std::cout << "train image count= " << trainImages.size() << std::endl;
		std::cout << "train label count= " << trainLabels.size() << std::endl;
		std::cout << "test image count= " << testImages.size() << std::endl;
		std::cout << "test label count= " << testLabels.size() << std::endl;

		NeuralNetwork nn({ 28 * 28,100,10 });
		std::vector<size_t> samples(testLabels.size());
		for (auto it = samples.begin(); it != samples.end(); ++it) {
			*it = samples.end() - it;
		}
		std::random_shuffle(samples.begin(), samples.end());
		samples.resize(100);

		for (size_t i = 0; i < trainImages.size(); i++) {
			nn.train(
				trainImages[i].toNormalizedVector(),
				trainLabels[i].toNormalizedVector(),
				0.1
			);
			if ((i % 100) == 0) {
				auto ok = 0;
				for (auto idx : samples) {
					nn.prediction(testImages[idx].toNormalizedVector());
					ok += (nn.maxOutputIndex() == testLabels[idx].label()) ? 1 : 0;
				}
				std::cout << i << ": " << ok << " / " << samples.size() << "(" << (100 * ok / samples.size()) << "%)" << std::endl;
			}
		}
		{
			auto ok = 0;
			for (size_t j = 0; j < testLabels.size(); j++) {
				nn.prediction(testImages[j].toNormalizedVector());
				ok += (nn.maxOutputIndex() == testLabels[j].label()) ? 1 : 0;
			}
			std::cout << "overall: " << ok << " / " << testLabels.size() << "(" << (100 * ok / testLabels.size()) << "%)" << std::endl;
		}

	}

	{
		std::complex<double> c1(1, 1);
		std::cout << c1 << std::endl;
		return 0;
	}
}
