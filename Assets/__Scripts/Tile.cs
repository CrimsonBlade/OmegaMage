using UnityEngine;
using System.Collections;

public class Tile : PT_MonoBehaviour {
	//public fields
	public string   type;

	//Hidden private fields
	private string  _tex;
	private int		_height = 0;
	private Vector3 _pos;

	//properties with get{} and set{}
	public int height{
		get{return (_height);}
		set{
			_height = value;
			AdjustHeight();
		}

	}

	//Sets the texture of the Tile based on a string
	//it requires LayoutTiles, so it's commented out for now

	public string tex{
		get {
			return(_tex);
		}
		set{
			_tex = value;
			name = "TilePrefab_"+_tex; //sets the name of this GameObject
			Texture2D t2D = LayoutTiles.S.GetTileTex(_tex);
			if(t2D == null){
				Utils.tr ("ERROR", "Tile.type{set}=",value,"No matching Texture2D in LayoutTiles.S.tileTextures!");
			}else{
				renderer.material.mainTexture = t2D;
			}
		}
	}


	//uses the 'new' keyword to replace the pos inherited from PT_MonoBehaviour
	//Without the 'new' keyword, the two properties would conflict
	new public Vector3 pos{
		get{return (_pos);}
		set{
			_pos = value;
			AdjustHeight();
		}
	}

	//Methods
	public void AdjustHeight(){
		//Moves the block up or down based on _height
		Vector3 vertOffset = Vector3.back * (_height - 0.5f);
		//the -0.5f shiffts the Tile down -0.5 units so that its top surface is 
		//at z=0 when pos.z=0 and height = 0
		transform.position = _pos + vertOffset;
	}
}
	
