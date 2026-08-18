# Prompts das 20 cartas da classe TANK — v2 (para o notebook cartas_colab_v3.ipynb)
#
# O que mudou em relação à v1:
#   1. CADA CARTA TEM UM ROSTO PRÓPRIO descrito (idade, cabelo, barba, pele, cicatriz).
#      Sem isso o modelo desenha sempre a mesma cara, e a referência de estilo piora o efeito.
#   2. METADE USA ELMO FECHADO. Rosto coberto = zero risco de olho torto, e fica ótimo em tank.
#   3. POSES QUE ESCONDEM A MÃO: arma no ombro, escudo plantado, braços cruzados, punho no peito.
#      Mão segurando cabo de arma é o pior ponto cego do SDXL.
#   4. Enquadramento da cintura pra cima (o "upper body" já vem no ESTILO do notebook).

PROMPTS = {

    # ---------- TIER 1 — recrutas, armadura simples, sem magia, rosto jovem ----------
    "tank_t1_vanguarda":        "a young clean-shaven human footman with short black hair and a scar across "
                                "his cheek, worn iron armor, round shield strapped to his forearm",

    "tank_t1_escudeiro_arcano": "a teenage squire with freckles and messy copper hair, steel armor etched with "
                                "glowing blue runes, arcane light reflecting on his face",

    "tank_t1_guarda_runico":    "a broad bald dwarf warrior with a thick braided red beard, dark rune-carved "
                                "plate, orange ember light rising from the runes, arms crossed",

    "tank_t1_penitente":        "a gaunt hooded penitent, sunken eyes and hollow cheeks, torn cloth over rusted "
                                "mail, iron chains around the neck, ash grey palette",

    "tank_t1_abencoado":        "a serene older woman knight with silver hair in a braid, eyes closed, simple "
                                "armor bathed in a shaft of golden light, warm halo",

    # ---------- TIER 2 — elite romana, uniforme, vermelho e bronze ----------
    "tank_t2_pretoriano":       "a praetorian guard in ornate crimson and bronze lorica, crested helmet with "
                                "cheek guards covering most of the face, stern jaw, imperial red banner",

    "tank_t2_legionario":       "a weathered roman legionary, olive skin, dark stubble, broken nose, segmented "
                                "iron armor, red tunic, tall shield planted in the ground beside him",

    "tank_t2_centuriao":        "a grey-haired centurion with a hard lined face and a scar through one eyebrow, "
                                "bronze armor, transverse crested helm under his arm",

    "tank_t2_guarda_costas":    "a huge dark-skinned bodyguard with a shaved head and calm eyes, thick plate "
                                "armor, arms spread wide protectively, massive shield behind him",

    # ---------- TIER 3 — heróis, aura mágica, começam os elmos fechados ----------
    "tank_t3_egide_arcana":     "a stern female knight with pale skin and white hair, silver plate, a floating "
                                "shield of blue arcane light in front of her, glowing glyphs orbiting",

    "tank_t3_irmao_de_armas":   "a grizzled veteran knight, deep wrinkles and a grey moustache, dented heavy "
                                "plate with wolf-shaped pauldrons, closed fist over his heart",

    "tank_t3_capitao_de_ferro": "an iron captain in blackened plate, closed horned helm hiding the face, glowing "
                                "eyes inside the visor, tattered war cape, embers in the air",

    "tank_t3_guardiao_da_fe":   "a holy guardian in white and gold plate, winged helm with a full visor, radiant "
                                "tower shield with a sunburst emblem, divine light",

    # ---------- TIER 4 — lendas, o efeito da carta aparece na arte ----------
    "tank_t4_baluarte":         "a colossal bulwark knight in fortress-like layered armor, faceless slit visor, "
                                "enormous wall shield, four elemental lights swirling around him",

    "tank_t4_porta_bandeira":   "a standard bearer in gleaming ornate armor, open helm framing a proud young "
                                "face, huge tattered war banner filling the background, golden dust",

    "tank_t4_quebra_golpes":    "a hulking defender in riveted iron armor, blank iron mask, shield raised to "
                                "intercept a blow, shockwave and shattered arrows, sparks",

    "tank_t4_tita_de_bronze":   "a towering bronze titan warrior, sculpted expressionless bronze face, verdigris "
                                "patina, glowing molten cracks, one arm raised in a taunt",

    # ---------- TIER 5 — mitos, presença esmagadora ----------
    "tank_t5_colosso":          "a gigantic colossus of carved stone and iron, no face, only glowing red cracks "
                                "where eyes would be, mountainous silhouette, dust storm",

    "tank_t5_tita_de_ferro":    "a monstrous iron titan in jagged black armor, molten core glowing through the "
                                "chest, steam venting from the joints, apocalyptic sky",

    "tank_t5_senhor_da_guerra": "a supreme warlord in golden black armor, weathered scarred face and a black "
                                "beard, spiked crown helm, arms crossed, army silhouettes behind",

}

# ---------------------------------------------------------------------------
# Se ainda vier rosto repetido: o problema é a referência de estilo.
# No notebook v3, célula 6 → MODO_REFERENCIA = "estilo".
#
# Se ainda vier mão torta: troque a pose por uma da lista abaixo, todas seguras.
#   "arms crossed" | "closed fist over his heart" | "shield planted in the ground"
#   "weapon resting on his shoulder" | "hands out of frame" | "one arm raised"
# ---------------------------------------------------------------------------
