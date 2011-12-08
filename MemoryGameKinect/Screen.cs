using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Coding4Fun.Kinect.Wpf.Controls;

namespace MemoryGameKinect
{
    public class Screen
    {
        public string reference;
        public string cardType;
        public int screenNumber;
        public HoverButton button;
        public Image image;

        public Screen()
        {
            this.reference = "";
            this.cardType = "";
            this.screenNumber = 0;
            this.button = null;
        }

        public Screen(string r, string type, int position, HoverButton b)
        {
            this.reference = r;
            this.cardType = type;
            this.screenNumber = position;
            this.button = b;
        }

        public string getReference()
        {
            return reference;
        }

        public string getCardType()
        {
            return cardType;
        }

        public int getPostion()
        {
            return screenNumber;
        }

        public HoverButton getButton()
        {
            return button;
        }

    }
}
