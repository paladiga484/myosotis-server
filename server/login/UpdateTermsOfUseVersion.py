from limbus.requests import Cs, ReqNull
from limbus.responses import Sc, RspNull
    

async def handle(req: Cs[ReqNull]):
    return Sc[RspNull](result=RspNull())
