import ezdxf
import os

def create_test_files(output_dir):
    os.makedirs(output_dir, exist_ok=True)

    # TEST 1 à 5 existants (réintégrés pour avoir tout le corpus de tests)
    doc1 = ezdxf.new()
    msp1 = doc1.modelspace()
    msp1.add_lwpolyline([(0, 0), (500, 0), (500, 400), (0, 400), (0, 0)])
    msp1.add_text("BUREAU 01").set_placement((250, 200))
    doc1.saveas(os.path.join(output_dir, "01_piece_simple.dxf"))

    doc2 = ezdxf.new()
    msp2 = doc2.modelspace()
    msp2.add_lwpolyline([(0, 0), (1000, 0), (1000, 600), (0, 600), (0, 0)])
    msp2.add_line((400, 0), (400, 600))
    msp2.add_line((400, 200), (1000, 200))
    msp2.add_text("CHAMBRE").set_placement((200, 300))
    msp2.add_text("SALLE DE BAIN").set_placement((700, 400))
    msp2.add_text("CIRCULATION").set_placement((700, 100))
    doc2.saveas(os.path.join(output_dir, "02_pieces_adjacentes.dxf"))

    # TEST 6: Pièce avec mobilier nommé "bureau" (pour tester la distinction IA)
    doc6 = ezdxf.new()
    msp6 = doc6.modelspace()
    msp6.add_lwpolyline([(0, 0), (500, 0), (500, 400), (0, 400), (0, 0)])
    msp6.add_text("CHAMBRE PARENTALE").set_placement((250, 300))
    msp6.add_lwpolyline([(50, 50), (150, 50), (150, 100), (50, 100), (50, 50)])
    msp6.add_text("bureau", dxfattribs={'height': 5}).set_placement((100, 75))
    doc6.saveas(os.path.join(output_dir, "06_piece_mobilier_bureau.dxf"))

    # TEST 7: Pièce avec porte de 90cm et gap de 5cm (erreur de dessin)
    doc7 = ezdxf.new()
    msp7 = doc7.modelspace()
    msp7.add_line((0, 0), (200, 0)) # Mur avec gap
    msp7.add_line((205, 0), (500, 0)) # Gap de 5cm non désiré
    msp7.add_line((500, 0), (500, 100))
    msp7.add_line((500, 190), (500, 400)) # Porte de 90cm
    msp7.add_line((500, 400), (0, 400))
    msp7.add_line((0, 400), (0, 0))
    msp7.add_text("BUREAU DIRECTION").set_placement((250, 200))
    doc7.saveas(os.path.join(output_dir, "07_gaps_et_portes.dxf"))

    # TEST 8: Murs sur calques multiples et poteau intérieur
    doc8 = ezdxf.new()
    doc8.layers.add("A-WALL", color=2)
    doc8.layers.add("A-COLS", color=3)
    msp8 = doc8.modelspace()
    msp8.add_line((0, 0), (500, 0), dxfattribs={'layer': 'A-WALL'})
    msp8.add_line((500, 0), (500, 400), dxfattribs={'layer': 'A-WALL'})
    msp8.add_line((500, 400), (0, 400), dxfattribs={'layer': 'A-WALL'})
    msp8.add_line((0, 400), (0, 0), dxfattribs={'layer': 'A-WALL'})
    # Poteau au milieu
    msp8.add_lwpolyline([(200, 200), (250, 200), (250, 250), (200, 250), (200, 200)], dxfattribs={'layer': 'A-COLS'})
    msp8.add_text("SALLE REUNION").set_placement((350, 300))
    doc8.saveas(os.path.join(output_dir, "08_calques_et_poteaux.dxf"))

    # TEST 9: Intersections imparfaites et doublons
    doc9 = ezdxf.new()
    msp9 = doc9.modelspace()
    msp9.add_line((0, 0), (500, 0))
    msp9.add_line((0, 0), (500, 0)) # Doublon exact
    msp9.add_line((500, -10), (500, 410)) # Intersection qui dépasse
    msp9.add_line((510, 400), (-10, 400)) # Intersection qui dépasse
    msp9.add_line((0, 410), (0, -10)) # Intersection qui dépasse
    msp9.add_text("LOCAL TECHNIQUE").set_placement((250, 200))
    doc9.saveas(os.path.join(output_dir, "09_intersections_doublons.dxf"))

    print(f"Fichiers de test DXF avancés générés dans : {output_dir}")

if __name__ == "__main__":
    create_test_files("tests/dxf")
