#include <iostream>
#include <fstream>
#include <string>
#include <thread>
#include <chrono>
#if defined(__linux__)
#include <unistd.h>
#include <sys/wait.h>
#endif

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
void progress_callback(int percentage, double speed, int remaining, int temp, bool healthy) {
    int width = 50; // Width of the progress bar
    int pos = width * percentage / 100;

    std::cout << CLEAR_L << BOLD << CYAN << "[";
    for (int i = 0; i < width; ++i) {
        if (i < pos) std::cout << "=";
        else if (i == pos) std::cout << ">";
        else std::cout << " ";
    }
    std::cout << "] " << percentage << "% ";

    // Formatting remaining time and temp
    int mins = remaining / 60;
    int secs = remaining % 60;

    std::string temp_color = temp >= 60 ? RED : GREEN;

    if (speed < 0.0) {
        printf("%s VERIFYING | %s ETA: %02d:%02d | %s Temp: %dC %s", YELLOW.c_str(), RED.c_str(), mins, secs, temp_color.c_str(), temp, RESET.c_str());
    } else {
        printf("%s %.1f MB/s | %s ETA: %02d:%02d | %s Temp: %dC %s", YELLOW.c_str(), speed, RED.c_str(), mins, secs, temp_color.c_str(), temp, RESET.c_str());
    }
    std::cout << std::flush;
}

// We will dynamically link to the shared C++ library
#if defined(_WIN32)
#define IMPORT __declspec(dllimport)
#else
#define IMPORT __attribute__((visibility("default")))
#endif

extern "C" {
    IMPORT int WriteIsoAsync(const char* target, const char* isoPath, bool isIsoMode, bool smartMonitor, bool verifyBlocks, void (*callback)(int, double, int, int, bool));
    IMPORT int FormatDisk(const char* target, bool quick, void (*callback)(int, double, int, int, bool));
}

int main(int argc, char* argv[]) {
#if defined(__linux__)
    if (geteuid() != 0) {
        std::cerr << BOLD << RED << "ERROR: diskf0rm4th requires root privileges to access raw block devices.\n" << RESET;
        std::cerr << "Please run with sudo.\n";
        return 1;
    }
#endif

    std::string locale = "en";
    std::string device = "";
    std::string iso = "";
    std::string fetch_url = "";

    // Simple arg parsing
    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        if (arg == "--lang" && i + 1 < argc) {
            locale = argv[++i];
        } else if (arg == "--fetch" && i + 1 < argc) {
            fetch_url = argv[++i];
        } else if (arg == "--device" && i + 1 < argc) {
            device = argv[++i];
        } else if (arg == "--iso" && i + 1 < argc) {
            iso = argv[++i];
        }
    }

    if (device.empty()) {
        std::cerr << BOLD << RED << "ERROR: Target device must be specified with --device (e.g., --device /dev/sdb).\n" << RESET;
        return 1;
    }

    if (iso.empty() && fetch_url.empty()) {
        std::cerr << BOLD << RED << "ERROR: ISO file must be specified with --iso or --fetch.\n" << RESET;
        return 1;
    }

    if (!fetch_url.empty()) {
        std::cout << BOLD << CYAN << "[Cloud Fetch] Downloading ISO from " << fetch_url << "..." << RESET << "\n";
        iso = "/tmp/diskform4th_cloud.iso";

        // Ensure no command injection by parsing arguments directly instead of system()
        pid_t pid = fork();
        if (pid == 0) {
            execlp("curl", "curl", "-L", "-o", iso.c_str(), fetch_url.c_str(), NULL);
            // If execlp returns, it failed
            std::cerr << BOLD << RED << "[Cloud Fetch] Exec failed." << RESET << "\n";
            exit(1);
        } else if (pid > 0) {
            int status;
            waitpid(pid, &status, 0);
            if (!WIFEXITED(status) || WEXITSTATUS(status) != 0) {
                std::cerr << BOLD << RED << "[Cloud Fetch] Download failed." << RESET << "\n";
                return 1;
            }
        } else {
            std::cerr << BOLD << RED << "[Cloud Fetch] Fork failed." << RESET << "\n";
            return 1;
        }

        std::cout << BOLD << GREEN << "[Cloud Fetch] Download complete." << RESET << "\n\n";
    }

    std::cout << BOLD << GREEN << "diskf0rm4th Native CLI v1.0" << RESET << "\n";
    std::cout << "--------------------------------\n";

    std::cout << get_localized_string("select_device", locale) << ": " << device << "\n";
    std::cout << get_localized_string("boot_selection", locale) << ": " << iso << "\n";
    std::cout << "--------------------------------\n";
    std::cout << BOLD << YELLOW << get_localized_string("status_burning", locale) << RESET << "\n";

    // Call the shared library
    // Use DD mode (false) by default for Linux CLI since it's typically used for ISOHybrid images
    WriteIsoAsync(device.c_str(), iso.c_str(), false, true, true, progress_callback);

    std::cout << "\n\n" << BOLD << GREEN << get_localized_string("status_done", locale) << "!" << RESET << "\n";
    return 0;
}