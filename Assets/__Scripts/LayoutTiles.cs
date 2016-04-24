using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TileTex{
	//this class enables us to define various textures for tiles
	public string str;
	public Texture2D tex;
}

public class LayoutTiles : MonoBehaviour {
	static public LayoutTiles S;

	public TextAsset roomsText; //the Rooms.xml file
	public string 	 roomNumber = "0"; //Current room # as a string
	// ^ roomNumber as string allows encoding in the XML & rooms 0-f
	public GameObject tilePrefab; //Prefab for all Tiles
	public TileTex[]  tileTextures; //A list of named textures for Tiles
	public GameObject portalPrefab; //prefab for the portals between rooms

	public bool _________________;

	private bool		  firstRoom = true; //is this the first room built?
	public PT_XMLReader   roomsXMLR;
	public PT_XMLHashList roomsXML;
	public Tile[,]		  tiles;
	public Transform	  tileAnchor;
	
	void Start () {
		S = this; // set the Singleton for LayoutTiles

		//make a new GameObect to be the TileAnchor (the parent transform of
		// all tiles).  this keeps Tiles tidy in the Hierarchy pane
		GameObject tAnc = new GameObject("TileAnchor");
		tileAnchor = tAnc.transform;

		//red the XML
		roomsXMLR = new PT_XMLReader (); //Creates a PT_XMLReader 
		roomsXMLR.Parse (roomsText.text); //parse the rooms.xml files
		roomsXML = roomsXMLR.xml["xml"][0]["room"]; // pull all the <room>s

		//build the 0th room
		BuildRoom (roomNumber);

	}

	//this is the GetTileTex() method that Tile uses
	public Texture2D GetTileTex(string tStr){
		//search through all the tileTextures for the proper string
		foreach (TileTex tTex in tileTextures) {
			if(tTex.str == tStr){
				return(tTex.tex);
			}		
		}
		//return null if nothing was found
		return(null);
	}

	//Build a room based on room number. this is an alternative version of
	// BuildRoom that grabs roomXML based on <room> num.
	public void BuildRoom(string rNumStr){
		PT_XMLHashtable roomHT = null;
		for (int i = 0; i<roomsXML.Count; i++) {
			PT_XMLHashtable ht = roomsXML[i];
			if(ht.att ("num") == rNumStr){
				roomHT = ht;
				break;
			}
		}
		if (roomHT == null) {
			Utils.tr ("ERROR", "LayoutTiles.BuildRoom()",
			          "Room not found: "+rNumStr);
			return;
		}
		BuildRoom (roomHT);
	}

	//Build a room from an XML <room> entry
	public void BuildRoom(PT_XMLHashtable room){
		//destroy any old tiles
		foreach (Transform t in tileAnchor) {//clear out old tiles
			// ^ you can iterate over a Trandrom to get its children
			Destroy(t.gameObject);
		}

		//move the mage out of the way
		Mage.S.pos = Vector3.left * 1000;
		// this keeps the mage from accidentally triggering OnTriggerExit() on
		// a portal. in my tsting, i found that OnTriggerExit was being called 
		// at strange times.
		Mage.S.ClearInput (); //Cancel any active mouse input and drags

		string rNumStr = room.att("num");

		//get the texture names for the floors and walls from <room> attributes
		string floorTexStr = room.att ("floor");
		string wallTexStr = room.att ("wall");
		//split the room into rows of tiles based on carriage returns in the
		// Rooms.xml file
		string[] roomRows = room.text.Split ('\n');
		//trim tabs from the beginnings of lines. however, we're leaving spaces
		// and underscores to allow for non-rectangular rooms
		for (int i = 0; i < roomRows.Length; i++) {
			roomRows[i] = roomRows[i].Trim ('\t');	
		}
		//clear the tiles array
		tiles = new Tile[ 100, 100 ]; //arbitrary max room size is 100x 100

		//declare a number of local fields that we'll use later
		Tile ti;
		string type, rawType, tileTexStr;
		GameObject go;
		int height;
		float maxY = roomRows.Length - 1;
		List<Portal> portals = new List<Portal> ();

		//these loops scan through each tile of each row of the room
		for (int y = 0; y < roomRows.Length; y++) {
			for(int x=0; x < roomRows[y].Length; x++){
				//set defaults
				height = 0;
				tileTexStr = floorTexStr;

				//get the character representing the tile
				type = rawType = roomRows[y][x].ToString();
				switch (rawType){
				case " ": //empty space
				case "_": //empty space
					//just skip over empty space
					continue;
					//Skips to the next iteration of the x loop
				case ".": //default floor
					//keep type="."
					break;
				case"|": // default wall
					height = 1;
					break;
				default:
					//anything else will be interpereted as floor
					type = ".";
					break;

				}

				// set the texture for floor or wall based on <room> attributes
				if(type == "." ){
					tileTexStr = floorTexStr;
				}else if (type == "|"){
					tileTexStr = wallTexStr;
				}

				//Instantiate a new TilePrefab
				go = Instantiate(tilePrefab) as GameObject;
				ti = go.GetComponent<Tile>();
				//set the parent Transform to tileAnchor
				ti.transform.parent = tileAnchor;
				//set the position of the tile
				ti.pos = new Vector3(x, maxY-y, 0);
				tiles[x,y] = ti;// add ti to the tiles 2d array

				//set the type, height, and texture of the Tile
				ti.type = type;
				ti.height = height;
				ti.tex = tileTexStr;

				//if the type is still rawType, continue to the next iteration 
				if(rawType == type) continue;

				//Check for specific entities in the room
				switch (rawType){
				case "X": //Starting spot for the Mage
					//Mage.S.pos = ti.pos;//uses the mage singlton  *** Line no longer used ***
					if(firstRoom){
						Mage.S.pos = ti.pos;//uses the mage singlton
						roomNumber = rNumStr;
						//  setting roomNumber now keeps any portals form
						//  moving the mage to them in this first room
						firstRoom = false;
					}
					break;

				case "0": //numbers are room portals (up to F in hexadecimal)
				case "1": // this allows portals to be placed in Rooms.xml file
				case "2":
				case "3":
				case "4":
				case "5":
				case "6":
				case "7":
				case "8":
				case "9":
				case "A":
				case "B":
				case "C":
				case "D":
				case "E":
				case "F":
					//Instantiate a portal
					GameObject pGO = Instantiate(portalPrefab) as GameObject;
					Portal p = pGO.GetComponent<Portal>();
					p.pos = ti.pos;
					p.transform.parent = tileAnchor;
					// attaching this to the tileAnchor means that the Portal 
					// will be destroyed when a new room is built
					p.toRoom = rawType;
					portals.Add(p);
					break;
				}

				//more to come here...
			}
		}

		//position the Mage
		foreach (Portal p in portals) {
			// if p.toRoom is the same as the room number the mage just exited,
			// then the mage should enter this room through this Portal
			// alternatively if firstRoom == true and there was no X in the 
			// room (as a defualt mage starting point), move the mage to this
			// portal as a backup measure (if, for instance, you want to just
			// load room number "5")
			if(p.toRoom == roomNumber || firstRoom){
				// if there is an x in the room, firstRoom will be set to false
				// by the time the code gets here
				Mage.S.StopWalking();  //stop any mage movement
				Mage.S.pos = p.pos; //move mage to this poratal location
				//mage maintains their facing from the previous room, so there
				// is no need to rotate them in order for them to enter this room
				// facing the right direction
				p.justArrived = true;
				// tell the Portal that mage has just arrived
				firstRoom = false;
				// stops s 2nd portal in this room from moving the mage to it
			}
		}

		// finaly assign the roomNumber
		roomNumber = rNumStr;
	}
}
