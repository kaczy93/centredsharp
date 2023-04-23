﻿namespace Shared;

public class GenericIndex : MulEntry {
    public const int Size = 12;
    public static GenericIndex Empty => new() { Lookup = 0, Length = -1, Various = 0 };
    public GenericIndex(BinaryReader? reader = null) {
        if (reader == null) return;
        
        Lookup = reader.ReadInt32();
        Length = reader.ReadInt32();
        Various = reader.ReadInt32();
    }

    public int Lookup { get; set; }
    public int Length { get; set; }
    public int Various { get; init; }

    public override void Write(BinaryWriter writer) {
        writer.Write(Lookup);
        writer.Write(Length);
        writer.Write(Various);
    }
}