using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retail_store
{
    public class Categ : Rayon
    {
        private string levelId;
        private Boolean leaf;

        public Categ(String name, String imageUrl, String id, String shopcode, String fatherId, String levelId, String leaf) : base(name, imageUrl, id, shopcode, fatherId)
        {
            this.LevelId = levelId;
            if (leaf == "false")
            {
                this.Leaf = false;
            }
            else
            {
                this.Leaf = true;
            }
            
        }

        public string LevelId
        {
            get
            {
                return levelId;
            }

            set
            {
                levelId = value;
            }
        }

        public Boolean Leaf
        {
            get
            {
                return leaf;
            }

            set
            {
                leaf = value;
            }
        }

        
    }
}
