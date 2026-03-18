#pragma once
#include <string>
#include <vector>

namespace qrcodegen {
    class QrCode {
    public:
        enum class Ecc { LOW, MEDIUM, QUARTILE, HIGH };
        static QrCode encodeText(const char* text, Ecc ecl) { return QrCode(); }
        int getSize() const { return 21; }
        bool getModule(int x, int y) const { return (x % 2 == y % 2); } // Fake matrix
    };
}
