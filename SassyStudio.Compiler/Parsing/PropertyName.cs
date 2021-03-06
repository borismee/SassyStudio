﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SassyStudio.Compiler.Parsing
{
    public class PropertyName : ComplexItem
    {
        public PropertyName()
        {
            Fragments = new ParseItemList();
        }

        public ParseItemList Fragments { get; protected set; }
        public override bool Parse(IItemFactory itemFactory, ITextProvider text, ITokenStream stream)
        {
            while (IsValidNameComponent(stream.Current.Type))
            {
                ParseItem fragement;
                if (itemFactory.TryCreateParsedOrDefault(this, text, stream, out fragement))
                {
                    Fragments.Add(fragement);
                    Children.Add(fragement);

                    if (fragement is TokenItem)
                        fragement.ClassifierType = SassClassifierType.PropertyName;
                }
            }

            return Children.Count > 0;
        }

        public override void Freeze()
        {
            base.Freeze();
            Fragments.TrimExcess();
        }

        public string GetName(ITextProvider text)
        {
            if (Fragments.Count == 0 || !Fragments.All(x => x is TokenItem))
                return "";

            var start = Fragments[0];
            var end = Fragments[Fragments.Count - 1];

            return text.GetText(start.Start, end.End - start.Start);
        }

        static bool IsValidNameComponent(TokenType type)
        {
            switch (type)
            {
                case TokenType.Identifier:
                case TokenType.OpenInterpolation:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsValidName(ITokenStream stream)
        {
            int start = stream.Position;
            var lastToken = stream.Current;
            bool valid = false;
            while (IsValidNameComponent(stream.Current.Type))
            {
                valid = true;
                if (stream.Advance().Start != lastToken.End)
                    break;

                lastToken = stream.Current;
            }

            stream.SeekTo(start);
            return valid;
        }
    }
}
