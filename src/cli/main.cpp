#include <iostream>
#include <fstream>
#include <string>
#include <thread>
#include <chrono>

// Note: In a real environment, we'd use a JSON library like nlohmann/json.
// For this prototype, we'll do simple string extraction from the locales files.
std::string get_localized_string(const std::string& key, const std::string& locale) {
    std::string filepath = "locales/" + locale + ".json";
    std::ifstream file(filepath);
    if (!file.is_open()) {
        filepath = "../../../locales/" + locale + ".json"; // Fallback for dev environment
        file.open(filepath);
    }

    if (file.is_open()) {
        std::string line;
        while (std::getline(file, line)) {
            size_t pos = line.find("\"" + key + "\"");
            if (pos != std::string::npos) {
                size_t colon = line.find(":", pos);
                size_t quote_start = line.find("\"", colon);
                size_t quote_end = line.find("\"", quote_start + 1);
                return line.substr(quote_start + 1, quote_end - quote_start - 1);
            }
        }
    }
    return "[" + key + "]";
}

// ANSI Escape Codes
const std::string RESET   = "\033[0m";
const std::string BOLD    = "\033[1m";
const std::string CYAN    = "\033[36m";
const std::string GREEN   = "\033[32m";
const std::string YELLOW  = "\033[33m";
const std::string RED     = "\033[31m";
const std::string CLEAR_L = "\033[2K\r"; // Clear line

// Mocked callback to match C-API
void progress_callback(int percentage, double speed, int remaining) {
    int width = 50; // Width of the progress bar
    int pos = width * percentage / 100;

    std::cout << CLEAR_L << BOLD << CYAN << "[";
    for (int i = 0; i < width; ++i) {
        if (i < pos) std::cout << "=";
        else if (i == pos) std::cout << ">";
        else std::cout << " ";
    }
    std::cout << "] " << percentage << "% ";

    // Formatting remaining time
    int mins = remaining / 60;
    int secs = remaining % 60;
    printf("%s %.1f MB/s | %s ETA: %02d:%02d%s", YELLOW.c_str(), speed, RED.c_str(), mins, secs, RESET.c_str());
    std::cout << std::flush;
}

// We will dynamically link to the shared C++ library
#if defined(_WIN32)
#define IMPORT __declspec(dllimport)
#else
#define IMPORT __attribute__((visibility("default")))
#endif

extern "C" {
    IMPORT int WriteIsoAsync(const char* target, const char* isoPath, bool smartMonitor, void (*callback)(int, double, int));
    IMPORT int FormatDisk(const char* target, bool quick, void (*callback)(int, double, int));
}

int main(int argc, char* argv[]) {
    std::string locale = "en";
    std::string device = "/dev/sdb";
    std::string iso = "ubuntu.iso";

    // Simple arg parsing
    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        if (arg == "--lang" && i + 1 < argc) {
            locale = argv[++i];
        }
    }

    std::cout << BOLD << GREEN << "diskf0rm4th Native CLI v1.0" << RESET << "\n";
    std::cout << "--------------------------------\n";

    std::cout << get_localized_string("select_device", locale) << ": " << device << "\n";
    std::cout << get_localized_string("boot_selection", locale) << ": " << iso << "\n";
    std::cout << "--------------------------------\n";
    std::cout << BOLD << YELLOW << get_localized_string("status_burning", locale) << RESET << "\n";

    // Call the shared library
    WriteIsoAsync(device.c_str(), iso.c_str(), true, progress_callback);

    std::cout << "\n\n" << BOLD << GREEN << get_localized_string("status_done", locale) << "!" << RESET << "\n";
    return 0;
}