//! The installer's unpacker (setup.c): a .zip into a folder, with the Zig standard library's own
//! zip reader and nothing else. Built for Windows next to setup.c; on Linux for the tests.
const std = @import("std");

pub const Progress = *const fn (done: u64, total: u64) callconv(.c) void;

var last_error: [:0]const u8 = "";

/// The zip at zip_path (UTF-8, absolute) into the existing empty folder dest_path. Files that
/// already exist there are refused. Returns 0, or 1 with lps_unzip_error() saying why.
export fn lps_unzip(zip_path: [*:0]const u8, dest_path: [*:0]const u8, progress: ?Progress) c_int {
    unzip(std.mem.span(zip_path), std.mem.span(dest_path), progress) catch |e| {
        last_error = @errorName(e);
        return 1;
    };
    return 0;
}

export fn lps_unzip_error() [*:0]const u8 {
    return last_error.ptr;
}

fn unzip(zip_path: []const u8, dest_path: []const u8, progress: ?Progress) !void {
    var threaded: std.Io.Threaded = .init_single_threaded;
    const io = threaded.io();
    const file = try std.Io.Dir.openFileAbsolute(io, zip_path, .{});
    defer file.close(io);
    var buffer: [64 * 1024]u8 = undefined;
    var reader = file.reader(io, &buffer);
    var dest = try std.Io.Dir.openDirAbsolute(io, dest_path, .{});
    defer dest.close(io);
    var it = try std.zip.Iterator.init(&reader);
    var name: [std.fs.max_path_bytes]u8 = undefined;
    var done: u64 = 0;
    while (try it.next()) |entry| {
        try entry.extract(&reader, .{}, &name, dest);
        done += 1;
        if (progress) |p| p(done, it.cd_record_count);
    }
}
