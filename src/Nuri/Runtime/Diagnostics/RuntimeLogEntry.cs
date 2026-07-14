using System;
using System.Collections.Generic;
using System.IO;

namespace Nuri.Runtime.Diagnostics
{
    public sealed class RuntimeLogEntry
    {
        public RuntimeLogEntry(
            long sequence,
            DateTimeOffset timestamp,
            RuntimeLogKind kind,
            string? rootId,
            string? componentId,
            string message,
            string? sourceType = null,
            string? sourceFile = null,
            string? sourceMember = null,
            int? sourceLine = null)
        {
            Sequence = sequence;
            Timestamp = timestamp;
            Kind = kind;
            RootId = rootId;
            ComponentId = componentId;
            Message = message;
            SourceType = sourceType;
            SourceFile = sourceFile;
            SourceMember = sourceMember;
            SourceLine = sourceLine;
        }

        public long Sequence { get; }

        public DateTimeOffset Timestamp { get; }

        public RuntimeLogKind Kind { get; }

        public string? RootId { get; }

        public string? ComponentId { get; }

        public string Message { get; }

        public string? SourceType { get; }

        public string? SourceFile { get; }

        public string? SourceMember { get; }

        public int? SourceLine { get; }

        public string LocalTime => Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");

        public string SourceDisplay
        {
            get
            {
                var source = !string.IsNullOrWhiteSpace(SourceType)
                    ? SourceType ?? string.Empty
                    : !string.IsNullOrWhiteSpace(SourceFile)
                        ? Path.GetFileName(SourceFile) ?? string.Empty
                        : string.Empty;

                if (!string.IsNullOrWhiteSpace(SourceMember))
                    source = string.IsNullOrWhiteSpace(source) ? SourceMember! : source + "." + SourceMember;

                if (SourceLine.HasValue && !string.IsNullOrWhiteSpace(source))
                    source += ":" + SourceLine.Value;

                return source;
            }
        }
    }

    public enum RuntimeLogKind
    {
        Diagnostics,
        RootRegistered,
        RootUnregistered,
        ComponentInvalidated,
        ComponentRendered,
        ComponentUnmounted,
        DuplicateKey,
        UnsupportedProperty,
        UnsupportedEvent,
        StoreChanged,
        SubtreeRebuild,
        FullRebuild,
        AppLog,
        Console
    }

    internal sealed class RuntimeLogBuffer
    {
        private readonly int _capacity;
        private readonly Queue<RuntimeLogEntry> _entries = new Queue<RuntimeLogEntry>();

        public RuntimeLogBuffer(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(RuntimeLogEntry entry)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _capacity)
                _entries.Dequeue();
        }

        public IReadOnlyList<RuntimeLogEntry> Snapshot()
        {
            return _entries.ToArray();
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
